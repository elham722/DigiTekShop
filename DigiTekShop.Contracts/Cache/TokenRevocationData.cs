namespace DigiTekShop.Contracts.Cache
{
    public sealed record TokenRevocationData
    {
        public required string Jti { get; init; }
        public required DateTime RevokedAt { get; init; }
        public required string Reason { get; init; }
        public required DateTime ExpiresAt { get; init; }
    }

}
