namespace DigiTekShop.Contracts.DTOs.Auth.Logout;
public sealed record LogoutRequest
{
    public string DeviceId { get; init; } = default!;

    public string? RefreshToken { get; init; }

    public string? UserAgent { get; init; }

    public string? Ip { get; init; }
}
