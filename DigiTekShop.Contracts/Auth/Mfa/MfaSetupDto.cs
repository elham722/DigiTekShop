namespace DigiTekShop.Contracts.Auth.Mfa
{
    public record MfaSetupDto(string QrCodeBase64, string SecretKey);
}
