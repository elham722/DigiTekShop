using DigiTekShop.SharedKernel.Enums.Verification;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Models;

public class PhoneVerification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }

    public string CodeHash { get; private set; } = null!;
    public int Attempts { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public string? PhoneNumber { get; private set; } // Display/Raw format
    public string? PhoneNumberNormalized { get; private set; } // E.164 format
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsVerified { get; private set; }

    public VerificationPurpose Purpose { get; private set; } = VerificationPurpose.Login;
    public VerificationChannel Channel { get; private set; } = VerificationChannel.Sms;

    public string? CodeHashAlgo { get; private set; }
    public int SecretVersion { get; private set; }
    public string? EncryptedCodeProtected { get; private set; }
    public string? DeviceId { get; private set; }

    public byte[]? RowVersion { get; private set; }

    private const int DefaultMaxAttempts = 5;
    private PhoneVerification() { }

    public static PhoneVerification CreateForUser(
        Guid userId,
        string codeHash,
        DateTimeOffset expiresAtUtc,
        string? phoneNumber = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        VerificationPurpose purpose = VerificationPurpose.Login,
        VerificationChannel channel = VerificationChannel.Sms,
        string? codeHashAlgo = "HMACSHA256",
        int secretVersion = 1,
        string? encryptedCodeProtected = null)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(codeHash, nameof(codeHash));
        var now = DateTimeOffset.UtcNow;
        Guard.AgainstPastDate(expiresAtUtc, () => now, nameof(expiresAtUtc));

        // Normalize phone number to E.164 format
        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            if (Normalization.TryNormalizePhoneIranE164(phoneNumber, out var e164))
            {
                normalizedPhone = e164;
            }
        }

        return new PhoneVerification
        {
            UserId = userId,
            CodeHash = codeHash,
            CodeHashAlgo = codeHashAlgo,
            SecretVersion = secretVersion,
            EncryptedCodeProtected = encryptedCodeProtected,
            Attempts = 0,
            // CreatedAtUtc will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
            ExpiresAtUtc = expiresAtUtc,
            PhoneNumber = StringNormalizer.NormalizeAndTruncate(phoneNumber, 32),
            PhoneNumberNormalized = normalizedPhone,
            IpAddress = StringNormalizer.NormalizeAndTruncate(ipAddress, 45),
            UserAgent = StringNormalizer.NormalizeAndTruncate(userAgent, 1024),
            DeviceId = StringNormalizer.NormalizeAndTruncate(deviceId, 128),
            Purpose = purpose,
            Channel = channel
        };
    }

    public void ResetCode(
        string newHash,
        DateTimeOffset newExpiresAtUtc,
        string? phoneNumber = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        string? codeHashAlgo = "HMACSHA256",
        int secretVersion = 1,
        string? encryptedCodeProtected = null)
    {
        Guard.AgainstNullOrEmpty(newHash, nameof(newHash));
        var now = DateTimeOffset.UtcNow;
        Guard.AgainstPastDate(newExpiresAtUtc, () => now, nameof(newExpiresAtUtc));

        CodeHash = newHash;
        CodeHashAlgo = codeHashAlgo;
        SecretVersion = secretVersion;
        EncryptedCodeProtected = encryptedCodeProtected;
        ExpiresAtUtc = newExpiresAtUtc;
        Attempts = 0;
        IsVerified = false;
        VerifiedAtUtc = null;
        LockedUntilUtc = null;

        // Normalize phone number to E.164 format
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            PhoneNumber = StringNormalizer.NormalizeAndTruncate(phoneNumber, 32);
            if (Normalization.TryNormalizePhoneIranE164(phoneNumber, out var e164))
            {
                PhoneNumberNormalized = e164;
            }
            else
            {
                PhoneNumberNormalized = null;
            }
        }

        if (!string.IsNullOrWhiteSpace(ipAddress)) IpAddress = StringNormalizer.NormalizeAndTruncate(ipAddress, 45);
        if (!string.IsNullOrWhiteSpace(userAgent)) UserAgent = StringNormalizer.NormalizeAndTruncate(userAgent, 1024);
        if (!string.IsNullOrWhiteSpace(deviceId)) DeviceId = StringNormalizer.NormalizeAndTruncate(deviceId, 128);
    }

    public bool TryIncrementAttempts(int maxAttempts = DefaultMaxAttempts, TimeSpan? lockDuration = null)
    {
        if (Attempts >= maxAttempts)
        {
            // Lock if max attempts reached
            if (lockDuration.HasValue)
            {
                LockedUntilUtc = DateTimeOffset.UtcNow.Add(lockDuration.Value);
            }
            return false;
        }

        Attempts++;
        return true;
    }

    public void ResetAttempts()
    {
        Attempts = 0;
        LockedUntilUtc = null;
    }

    public void MarkAsVerified(DateTimeOffset nowUtc)
    {
        IsVerified = true;
        VerifiedAtUtc = nowUtc;
    }

    public void UpdateRequestInfo(string? ipAddress = null, string? userAgent = null)
    {
        if (!string.IsNullOrWhiteSpace(ipAddress)) IpAddress = StringNormalizer.NormalizeAndTruncate(ipAddress, 45);
        if (!string.IsNullOrWhiteSpace(userAgent)) UserAgent = StringNormalizer.NormalizeAndTruncate(userAgent, 1024);
    }

    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;
    public bool IsLocked(DateTimeOffset nowUtc) => LockedUntilUtc.HasValue && nowUtc < LockedUntilUtc.Value;
    public bool IsValid(DateTimeOffset nowUtc, int maxAttempts = DefaultMaxAttempts)
        => !IsExpired(nowUtc) && !IsLocked(nowUtc) && !IsVerified && Attempts < maxAttempts;
}
