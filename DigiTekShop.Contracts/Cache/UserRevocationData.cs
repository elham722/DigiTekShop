namespace DigiTekShop.Contracts.Cache
{
    public sealed record UserRevocationData
    {
        public required Guid UserId { get; init; }
        public required DateTime RevokedAt { get; init; }
        public required string Reason { get; init; }
    }
}
