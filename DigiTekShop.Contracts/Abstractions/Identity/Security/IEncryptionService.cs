using DigiTekShop.SharedKernel.Enums.Security;

namespace DigiTekShop.Contracts.Abstractions.Identity.Security;
public interface IEncryptionService
{
    string Encrypt(string plainText, CryptoPurpose purpose = CryptoPurpose.TotpSecret);
    string Decrypt(string encryptedText, CryptoPurpose purpose = CryptoPurpose.TotpSecret);
    bool TryDecrypt(string encryptedText, out string? plainText, CryptoPurpose purpose = CryptoPurpose.TotpSecret);

    string EncryptBytes(ReadOnlySpan<byte> data, CryptoPurpose purpose = CryptoPurpose.TotpSecret);
    byte[] DecryptToBytes(string encryptedText, CryptoPurpose purpose = CryptoPurpose.TotpSecret);
}

