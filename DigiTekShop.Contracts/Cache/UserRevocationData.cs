namespace DigiTekShop.Contracts.Cache
{
    public sealed record UserRevocationData(
        Guid UserId,
        DateTime RevokedAt,
        string Reason
    );
}
