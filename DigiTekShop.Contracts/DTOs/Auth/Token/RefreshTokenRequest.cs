namespace DigiTekShop.Contracts.DTOs.Auth.Token
{
    public sealed record RefreshTokenRequest
    {
        public string RefreshToken { get; init; } = default!;

        public string DeviceId { get; init; } = default!;

        public string? UserAgent { get; init; }

        public string? Ip { get; init; }
    }
}
