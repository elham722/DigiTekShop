using DigiTekShop.SharedKernel.Enums.Security;
using DigiTekShop.SharedKernel.Guards;

namespace DigiTekShop.Identity.Models;


public sealed class SecurityEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }
    public SecurityEventType Type { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceId { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;
    public bool IsResolved { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private SecurityEvent() { }

    public static SecurityEvent Create(
        SecurityEventType type,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        string? metadataJson = null)
    {
        Guard.AgainstEmpty(type, nameof(type));

        return new SecurityEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceId = deviceId,
            MetadataJson = metadataJson,
            OccurredAt = DateTime.UtcNow,
            IsResolved = false
        };
    }

    public static SecurityEvent CreateWithMetadata<T>(
        SecurityEventType type,
        T metadata,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null)
    {
        var metadataJson = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null;
        
        return Create(type, userId, ipAddress, userAgent, deviceId, metadataJson);
    }

    public void Resolve(string resolvedBy, string? resolutionNotes = null)
    {
        Guard.AgainstNullOrEmpty(resolvedBy, nameof(resolvedBy));

        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        ResolutionNotes = resolutionNotes;
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
