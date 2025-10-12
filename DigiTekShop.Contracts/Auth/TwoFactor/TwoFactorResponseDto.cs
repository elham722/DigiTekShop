namespace DigiTekShop.Contracts.Auth.TwoFactor
{
    public record TwoFactorResponseDto(bool Enabled, TwoFactorProvider Provider);
}
