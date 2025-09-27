using DigiTekShop.Identity.Enums;
using DigiTekShop.SharedKernel.Guards;
using System;

namespace DigiTekShop.Identity.Models
{
    public class AuditLog
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid UserId { get; private set; }
        public AuditAction Action { get; private set; }
        public string EntityName { get; private set; } = null!;
        public string EntityId { get; private set; } = null!;
        public string? OldValue { get; private set; }
        public string? NewValue { get; private set; }
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public bool IsSuccess { get; private set; }
        public string? ErrorMessage { get; private set; }
        public AuditSeverity Severity { get; private set; } = AuditSeverity.Info;

        // Optional Security fields
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }

        private AuditLog() { }

        public static AuditLog Create(Guid userId, AuditAction action, string entityName, string entityId,
            string? oldValues = null, string? newValues = null, string? ipAddress = null, string? userAgent = null,
            string? sessionId = null, string? requestId = null, string? additionalData = null, bool isSuccess = true,
            string? errorMessage = null, AuditSeverity? severity = null)
        {
            Guard.AgainstEmpty(userId, nameof(userId));
            Guard.AgainstEmpty(action, nameof(action));
            Guard.AgainstNullOrEmpty(entityName, nameof(entityName));
            Guard.AgainstNullOrEmpty(entityId, nameof(entityId));
            var finalSeverity = severity ?? (isSuccess ? AuditSeverity.Info : AuditSeverity.Warning);
            return new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValue = oldValues,
                NewValue = newValues,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Severity = finalSeverity
            };
        }
    
    }
}