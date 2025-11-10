using DigiTekShop.SharedKernel.Enums.Audit;
using DigiTekShop.SharedKernel.Enums.Security;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Utilities.Security;

namespace DigiTekShop.Identity.Models;

public sealed class SecurityEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }
    public SecurityEventType Type { get; private set; }
    public AuditSeverity Severity { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceId { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? ResolutionNotes { get; private set; }

    // Correlation fields for request tracking
    public string? CorrelationId { get; private set; }
    public string? RequestId { get; private set; }
    public Guid? AuditLogId { get; private set; }

    private SecurityEvent() { }

    public static SecurityEvent Create(
        SecurityEventType type,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        string? metadataJson = null,
        string? correlationId = null,
        string? requestId = null,
        Guid? auditLogId = null)
    {
        // Type is enum, no need for Guard check

        // Derive severity from type
        var severity = DeriveSeverity(type);

        // Redact sensitive fields from metadata JSON
        var redactedMetadata = JsonRedactor.RedactSensitiveFields(metadataJson);

        // Normalize and truncate string fields
        var normalizedIp = NormalizeAndTruncate(ipAddress, 45);
        var normalizedUserAgent = NormalizeAndTruncate(userAgent, 1024);
        var normalizedDeviceId = NormalizeAndTruncate(deviceId, 128);
        var normalizedCorrelationId = NormalizeAndTruncate(correlationId, 128);
        var normalizedRequestId = NormalizeAndTruncate(requestId, 128);

        return new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Severity = severity,
            IpAddress = normalizedIp,
            UserAgent = normalizedUserAgent,
            DeviceId = normalizedDeviceId,
            MetadataJson = redactedMetadata,
            // OccurredAt will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
            IsResolved = false,
            CorrelationId = normalizedCorrelationId,
            RequestId = normalizedRequestId,
            AuditLogId = auditLogId
        };
    }

    private static AuditSeverity DeriveSeverity(SecurityEventType type)
    {
        return type switch
        {
            SecurityEventType.SystemIntrusion or
            SecurityEventType.DataBreach or
            SecurityEventType.BruteForceAttempt or
            SecurityEventType.TokenReplay or
            SecurityEventType.DeviceSuspicious or
            SecurityEventType.UnauthorizedAccess => AuditSeverity.Critical,
            SecurityEventType.LoginFailed or
            SecurityEventType.AccountLocked or
            SecurityEventType.MfaFailed or
            SecurityEventType.RefreshTokenAnomaly or
            SecurityEventType.DeviceUntrusted or
            SecurityEventType.PermissionDenied or
            SecurityEventType.RateLimitExceeded => AuditSeverity.Warning,
            _ => AuditSeverity.Info
        };
    }

    private static string? NormalizeAndTruncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return null;

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    public static SecurityEvent CreateWithMetadata<T>(
        SecurityEventType type,
        T metadata,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        string? correlationId = null,
        string? requestId = null,
        Guid? auditLogId = null)
    {
        var metadataJson = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null;
        
        return Create(type, userId, ipAddress, userAgent, deviceId, metadataJson, correlationId, requestId, auditLogId);
    }

    public void Resolve(string resolvedBy, string? resolutionNotes = null)
    {
        Guard.AgainstNullOrEmpty(resolvedBy, nameof(resolvedBy));

        IsResolved = true;
        ResolvedAt = DateTimeOffset.UtcNow;
        ResolvedBy = NormalizeAndTruncate(resolvedBy, 256);
        ResolutionNotes = NormalizeAndTruncate(resolutionNotes, 2000);
    }

    public void Unresolve()
    {
        IsResolved = false;
        ResolvedAt = null;
        ResolvedBy = null;
        ResolutionNotes = null;
    }

    public T? GetMetadata<T>()
    {
        if (string.IsNullOrWhiteSpace(MetadataJson))
            return default;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(MetadataJson);
        }
        catch
        {
            return default;
        }
    }

    public bool IsHighSeverity => Type switch
    {
        SecurityEventType.SystemIntrusion or
        SecurityEventType.DataBreach or
        SecurityEventType.BruteForceAttempt or
        SecurityEventType.TokenReplay or
        SecurityEventType.DeviceSuspicious or
        SecurityEventType.UnauthorizedAccess => true,
        _ => false
    };

    public bool IsMediumSeverity => Type switch
    {
        SecurityEventType.LoginFailed or
        SecurityEventType.AccountLocked or
        SecurityEventType.MfaFailed or
        SecurityEventType.RefreshTokenAnomaly or
        SecurityEventType.DeviceUntrusted or
        SecurityEventType.PermissionDenied or
        SecurityEventType.RateLimitExceeded => true,
        _ => false
    };

    public bool IsLowSeverity => !IsHighSeverity && !IsMediumSeverity;
}
