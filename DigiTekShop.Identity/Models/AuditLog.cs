using DigiTekShop.SharedKernel.Enums.Audit;
using DigiTekShop.SharedKernel.Utilities.Security;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Models;

public sealed class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ActorId { get; private set; }
    public ActorType ActorType { get; private set; } = ActorType.User;
    public AuditAction Action { get; private set; }
    public string TargetEntityName { get; private set; } = null!;
    public string TargetEntityId { get; private set; } = null!;
    public string? OldValueJson { get; private set; }
    public string? NewValueJson { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public AuditSeverity Severity { get; private set; } = AuditSeverity.Info;

    // Security fields for context
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceId { get; private set; }

    // Correlation fields for request tracking
    public string? CorrelationId { get; private set; }
    public string? RequestId { get; private set; }
    public string? SessionId { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        Guid actorId,
        AuditAction action,
        string targetEntityName,
        string targetEntityId,
        string? oldValueJson = null,
        string? newValueJson = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        bool isSuccess = true,
        string? errorMessage = null,
        AuditSeverity? severity = null,
        ActorType actorType = ActorType.User,
        string? correlationId = null,
        string? requestId = null,
        string? sessionId = null)
    {
        Guard.AgainstEmpty(actorId, nameof(actorId));
        Guard.AgainstNullOrEmpty(targetEntityName, nameof(targetEntityName));
        Guard.AgainstNullOrEmpty(targetEntityId, nameof(targetEntityId));

        // Redact sensitive fields from JSON
        var redactedOldJson = JsonRedactor.RedactSensitiveFields(oldValueJson);
        var redactedNewJson = JsonRedactor.RedactSensitiveFields(newValueJson);

        // Normalize and truncate string fields
        var normalizedTargetName = StringNormalizer.NormalizeAndTruncate(targetEntityName, 256);
        var normalizedTargetId = StringNormalizer.NormalizeAndTruncate(targetEntityId, 256);
        var normalizedIp = StringNormalizer.NormalizeAndTruncate(ipAddress, 45);
        var normalizedUserAgent = StringNormalizer.NormalizeAndTruncate(userAgent, 1024);
        var normalizedDeviceId = StringNormalizer.NormalizeAndTruncate(deviceId, 128);
        var normalizedError = StringNormalizer.NormalizeAndTruncate(errorMessage, 1024);
        var normalizedCorrelationId = StringNormalizer.NormalizeAndTruncate(correlationId, 128);
        var normalizedRequestId = StringNormalizer.NormalizeAndTruncate(requestId, 128);
        var normalizedSessionId = StringNormalizer.NormalizeAndTruncate(sessionId, 128);

        var finalSeverity = severity ?? (isSuccess ? AuditSeverity.Info : AuditSeverity.Warning);
        
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActorType = actorType,
            Action = action,
            TargetEntityName = normalizedTargetName,
            TargetEntityId = normalizedTargetId,
            OldValueJson = redactedOldJson,
            NewValueJson = redactedNewJson,
            // Timestamp will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
            IsSuccess = isSuccess,
            ErrorMessage = normalizedError,
            Severity = finalSeverity,
            IpAddress = normalizedIp,
            UserAgent = normalizedUserAgent,
            DeviceId = normalizedDeviceId,
            CorrelationId = normalizedCorrelationId,
            RequestId = normalizedRequestId,
            SessionId = normalizedSessionId
        };
    }

    public void UpdateResult(bool isSuccess, string? errorMessage = null, AuditSeverity? severity = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = StringNormalizer.NormalizeAndTruncate(errorMessage, 1024);
        Severity = severity ?? (isSuccess ? AuditSeverity.Info : AuditSeverity.Warning);
    }
}