using DigiTekShop.Contracts.Enums.Security;

namespace DigiTekShop.Contracts.Auth.SecurityEvent
{
    public sealed record SecurityEventDto(
        Guid Id,
        SecurityEventType EventType,
        Guid? UserId,
        string? IpAddress,
        string? UserAgent,
        string? DeviceId,
        string? MetadataJson,
        DateTime OccurredAt,
        bool IsResolved,
        DateTime? ResolvedAt,
        string? ResolvedBy,
        string? ResolutionNotes,
        string? RiskLevel
    );
}
