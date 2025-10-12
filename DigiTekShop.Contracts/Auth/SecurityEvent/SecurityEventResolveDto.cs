namespace DigiTekShop.Contracts.Auth.SecurityEvent
{
    public sealed class SecurityEventResolveDto
    {
        public Guid EventId { get; init; }
        public string ResolvedBy { get; init; } = default!;
        public string? ResolutionNotes { get; init; }
    }
}
