using DigiTekShop.SharedKernel.Enums.Security;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace DigiTekShop.Identity.Services.Security;

public sealed class EncryptionService : IEncryptionService
{
    private readonly IDataProtectionProvider _provider;
    private readonly ILogger<EncryptionService> _logger;

    private const string RootPurpose = "DigiTekShop.Crypto";
    private static string Purpose(CryptoPurpose p) => $"{RootPurpose}:{p}";

    public EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Encrypt(string plainText, CryptoPurpose purpose = CryptoPurpose.TotpSecret)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("plainText is empty", nameof(plainText));

        try
        {
            var protector = _provider.CreateProtector(Purpose(purpose));
            return protector.Protect(plainText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encrypt failed (purpose={Purpose})", purpose);
            throw new CryptographicException("Encryption failed", ex);
        }
    }

    public string Decrypt(string encryptedText, CryptoPurpose purpose = CryptoPurpose.TotpSecret)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
            throw new ArgumentException("encryptedText is empty", nameof(encryptedText));

        try
        {
            var protector = _provider.CreateProtector(Purpose(purpose));
            return protector.Unprotect(encryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decrypt failed (purpose={Purpose})", purpose);
            throw new CryptographicException("Decryption failed", ex);
        }
    }

    public bool TryDecrypt(string encryptedText, out string? plainText, CryptoPurpose purpose = CryptoPurpose.TotpSecret)
    {
        plainText = null;
        if (string.IsNullOrWhiteSpace(encryptedText)) return false;

        try
        {
            var protector = _provider.CreateProtector(Purpose(purpose));
            plainText = protector.Unprotect(encryptedText);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string EncryptBytes(ReadOnlySpan<byte> data, CryptoPurpose purpose = CryptoPurpose.TotpSecret)
    {
        if (data.IsEmpty) throw new ArgumentException("data is empty", nameof(data));
        var base64 = Convert.ToBase64String(data);
        return Encrypt(base64, purpose);
    }

    public byte[] DecryptToBytes(string encryptedText, CryptoPurpose purpose = CryptoPurpose.TotpSecret)
    {
        var base64 = Decrypt(encryptedText, purpose);
        return Convert.FromBase64String(base64);
    }
}
