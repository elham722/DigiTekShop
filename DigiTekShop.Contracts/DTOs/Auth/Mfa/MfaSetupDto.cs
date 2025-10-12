namespace DigiTekShop.Contracts.DTOs.Auth.Mfa
{
    public record MfaSetupDto(string QrCodeBase64, string SecretKey);
}
