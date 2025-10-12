namespace DigiTekShop.Contracts.Auth.SecurityEvent
{
    public sealed record SecurityEventResolveDto(
        Guid EventId,
        string ResolvedBy,
        string? ResolutionNotes
    );
}
