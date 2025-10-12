namespace DigiTekShop.Contracts.Auth.Logout
{
    public record LogoutRequestDto(
        string? RefreshToken = null,
        string? AccessToken = null
    );
}
