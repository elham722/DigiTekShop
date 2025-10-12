namespace DigiTekShop.Contracts.DTOs.Auth.Logout
{
    public record LogoutRequestDto(
        string? RefreshToken = null,
        string? AccessToken = null
    );
}
