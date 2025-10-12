namespace DigiTekShop.Contracts.DTOs.Auth.TwoFactor
{
    public record TwoFactorResponseDto(bool Enabled, TwoFactorProvider Provider);
}
