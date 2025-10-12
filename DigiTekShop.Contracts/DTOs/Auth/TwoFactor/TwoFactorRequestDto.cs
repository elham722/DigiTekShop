namespace DigiTekShop.Contracts.DTOs.Auth.TwoFactor
{
    public record TwoFactorRequestDto(string UserId, TwoFactorProvider Provider);
}
