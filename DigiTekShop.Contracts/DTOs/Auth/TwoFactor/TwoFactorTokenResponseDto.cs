namespace DigiTekShop.Contracts.DTOs.Auth.TwoFactor
{
    public record TwoFactorTokenResponseDto(string Token, DateTimeOffset ExpiresAt);
}
