using DigiTekShop.Contracts.Enums.Security;

namespace DigiTekShop.Contracts.Auth.SecurityEvent
{
    public sealed class SecurityEventCreateDto
    {
        public SecurityEventType EventType { get; init; }
        public Guid? UserId { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string? DeviceId { get; init; }
        public string? MetadataJson { get; init; }
    }
}
