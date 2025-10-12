namespace DigiTekShop.Contracts.Cache
{
    public sealed record TokenRevocationData(
        string Jti,
        DateTime RevokedAt,
        string Reason,
        DateTime ExpiresAt
    );
}
