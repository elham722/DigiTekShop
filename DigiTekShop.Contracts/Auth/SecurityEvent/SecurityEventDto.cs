using DigiTekShop.Contracts.Enums.Security;

namespace DigiTekShop.Contracts.Auth.SecurityEvent
{
    public sealed class SecurityEventDto
    {
        public Guid Id { get; init; }
        public SecurityEventType EventType { get; init; }
        public Guid? UserId { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string? DeviceId { get; init; }

        public string? MetadataJson { get; init; }

        public DateTime OccurredAt { get; init; }

        public bool IsResolved { get; init; }
        public DateTime? ResolvedAt { get; init; }
        public string? ResolvedBy { get; init; }
        public string? ResolutionNotes { get; init; }

        public string? RiskLevel { get; init; }
    }
}
