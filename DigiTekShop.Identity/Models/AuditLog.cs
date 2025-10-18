using DigiTekShop.SharedKernel.Enums.Audit;

namespace DigiTekShop.Identity.Models;


public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ActorId { get; private set; }
    public string ActorType { get; private set; } = "User"; // User, Service, System
    public AuditAction Action { get; private set; }
    public string TargetEntityName { get; private set; } = null!;
    public string TargetEntityId { get; private set; } = null!;
    public string? OldValueJson { get; private set; }
    public string? NewValueJson { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public AuditSeverity Severity { get; private set; } = AuditSeverity.Info;

    // Security fields for context
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceId { get; private set; }

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
        string actorType = "User")
    {
        Guard.AgainstEmpty(actorId, nameof(actorId));
        Guard.AgainstNullOrEmpty(targetEntityName, nameof(targetEntityName));
        Guard.AgainstNullOrEmpty(targetEntityId, nameof(targetEntityId));

        var finalSeverity = severity ?? (isSuccess ? AuditSeverity.Info : AuditSeverity.Warning);
        
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActorType = actorType,
            Action = action,
            TargetEntityName = targetEntityName,
            TargetEntityId = targetEntityId,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            Timestamp = DateTime.UtcNow,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            Severity = finalSeverity,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceId = deviceId
        };
    }

    public void UpdateResult(bool isSuccess, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Severity = isSuccess ? AuditSeverity.Info : AuditSeverity.Warning;
    }
}