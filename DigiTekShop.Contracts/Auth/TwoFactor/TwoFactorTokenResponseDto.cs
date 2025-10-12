namespace DigiTekShop.Contracts.Auth.TwoFactor
{
    public record TwoFactorTokenResponseDto(string Token, DateTimeOffset ExpiresAt);
}
