namespace DigiTekShop.Contracts.DTOs.Auth.Logout;
public sealed record LogoutRequest
{
    public Guid UserId { get; init; }
    public string? RefreshToken { get; init; }
}
