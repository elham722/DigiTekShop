using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using DigiTekShop.Contracts.Abstractions.Identity.Encryption;

namespace DigiTekShop.Identity.Services;



public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger)
    {
        _protector = provider.CreateProtector("DigiTekShop.TotpSecrets");
        _logger = logger;
    }

    public string Encrypt(string plainText)
    {
        Guard.AgainstNullOrEmpty(plainText, nameof(plainText));
        try
        {
            return _protector.Protect(plainText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt TOTP secret");
            throw new CryptographicException("Encryption failed", ex);
        }
    }

    public string Decrypt(string encryptedText)
    {
        Guard.AgainstNullOrEmpty(encryptedText, nameof(encryptedText));
        try
        {
            return _protector.Unprotect(encryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt TOTP secret");
            throw new CryptographicException("Decryption failed", ex);
        }
    }
}
