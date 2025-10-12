namespace DigiTekShop.Contracts.Auth.TwoFactor
{
    public record TwoFactorRequestDto(string UserId, TwoFactorProvider Provider);
}
