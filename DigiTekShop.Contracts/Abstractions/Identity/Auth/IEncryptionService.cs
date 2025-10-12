namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);

        string Decrypt(string encryptedText);
    }
}
