namespace DigiTekShop.Contracts.DTOs.Auth.Token
{
    public sealed record RefreshTokenRequest
    {
        public string RefreshToken { get; init; } = default!;
    }
}
