using DigiTekShop.SharedKernel.Enums.Security;

namespace DigiTekShop.Contracts.DTOs.Auth.SecurityEvent
{
    public sealed record SecurityEventDto(
        Guid Id,
        SecurityEventType EventType,
        Guid? UserId,
        string? IpAddress,
        string? UserAgent,
        string? DeviceId,
        string? MetadataJson,
        DateTimeOffset OccurredAt,
        bool IsResolved,
        DateTimeOffset? ResolvedAt,
        string? ResolvedBy,
        string? ResolutionNotes,
        string? RiskLevel
    );
}
