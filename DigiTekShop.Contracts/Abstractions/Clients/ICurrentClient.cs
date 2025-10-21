namespace DigiTekShop.Contracts.Abstractions.Clients
{
    public interface ICurrentClient
    {
        string? DeviceId { get; }
        string? UserAgent { get; }
        string? IpAddress { get; }

        string? AccessTokenRaw { get; }
        string? AccessTokenJti { get; }
        Guid? AccessTokenSubject { get; }
        DateTime? AccessTokenIssuedAtUtc { get; }
        DateTime? AccessTokenExpiresAtUtc { get; }
    }
}
