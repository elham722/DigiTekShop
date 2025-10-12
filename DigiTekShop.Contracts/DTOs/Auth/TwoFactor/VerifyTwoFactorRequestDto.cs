namespace DigiTekShop.Contracts.DTOs.Auth.TwoFactor
{
    public record VerifyTwoFactorRequestDto(string UserId, TwoFactorProvider Provider, string Code);
}
