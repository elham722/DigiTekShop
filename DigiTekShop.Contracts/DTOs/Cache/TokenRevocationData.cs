namespace DigiTekShop.Contracts.DTOs.Cache
{
    public sealed record TokenRevocationData(
        string Jti,
        DateTime RevokedAt,
        string Reason,
        DateTime ExpiresAt
    );
}
