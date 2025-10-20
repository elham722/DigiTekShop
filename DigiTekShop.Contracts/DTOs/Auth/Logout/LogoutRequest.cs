namespace DigiTekShop.Contracts.DTOs.Auth.Logout;
public sealed record LogoutRequest
{
    public string? RefreshToken { get; init; }
}
