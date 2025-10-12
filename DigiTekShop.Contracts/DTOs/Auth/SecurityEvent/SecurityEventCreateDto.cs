namespace DigiTekShop.Contracts.DTOs.Auth.SecurityEvent
{
    public sealed record SecurityEventCreateDto(
        SecurityEventType EventType,
        Guid? UserId,
        string? IpAddress,
        string? UserAgent,
        string? DeviceId,
        string? MetadataJson
    );
}
