namespace DigiTekShop.Contracts.DTOs.Cache
{
    public sealed record UserRevocationData(
        Guid UserId,
        DateTime RevokedAt,
        string Reason
    );
}
