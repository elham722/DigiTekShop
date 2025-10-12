namespace DigiTekShop.Contracts.Auth.TwoFactor
{
    public record VerifyTwoFactorRequestDto(string UserId, TwoFactorProvider Provider, string Code);
}
