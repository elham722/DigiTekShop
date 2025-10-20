using DigiTekShop.SharedKernel.Exceptions.Validation;

namespace DigiTekShop.Identity.Models;

public class PhoneVerification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; } = null!;
    public int Attempts { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }

    public string? PhoneNumber { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsVerified { get; private set; }

    public byte[]? RowVersion { get; private set; }

    private const int DefaultMaxAttempts = 5;

    private PhoneVerification() { }

    public static PhoneVerification Create(
        Guid userId,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string? phoneNumber = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(codeHash, nameof(codeHash));
        Guard.AgainstPastDate(expiresAtUtc, () => DateTime.UtcNow, nameof(expiresAtUtc));

        return new PhoneVerification
        {
            UserId = userId,
            CodeHash = codeHash,
            Attempts = 0,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            PhoneNumber = TrimTo(phoneNumber, 32),        
            IpAddress = TrimTo(ipAddress, 45),         
            UserAgent = TrimTo(userAgent, 256)
        };
    }

    public void ResetCode(
        string newHash,
        DateTime newCreatedAtUtc,
        DateTime newExpiresAtUtc,
        string? phoneNumber = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        Guard.AgainstNullOrEmpty(newHash, nameof(newHash));
        Guard.AgainstPastDate(newExpiresAtUtc, () => DateTime.UtcNow, nameof(newExpiresAtUtc));

        CodeHash = newHash;
        CreatedAtUtc = newCreatedAtUtc;
        ExpiresAtUtc = newExpiresAtUtc;
        Attempts = 0;
        IsVerified = false;
        VerifiedAtUtc = null;

        if (!string.IsNullOrWhiteSpace(phoneNumber)) PhoneNumber = TrimTo(phoneNumber, 32);
        if (!string.IsNullOrWhiteSpace(ipAddress)) IpAddress = TrimTo(ipAddress, 45);
        if (!string.IsNullOrWhiteSpace(userAgent)) UserAgent = TrimTo(userAgent, 256);
    }

    public bool TryIncrementAttempts(int maxAttempts = DefaultMaxAttempts)
    {
        if (Attempts >= maxAttempts) return false;
        Attempts++;
        return true;
    }

    public void ResetAttempts() => Attempts = 0;

    public void MarkAsVerified(DateTime nowUtc)
    {
        IsVerified = true;
        VerifiedAtUtc = nowUtc;
    }

    public void UpdateRequestInfo(string? ipAddress = null, string? userAgent = null)
    {
        if (!string.IsNullOrWhiteSpace(ipAddress))
            IpAddress = TrimTo(ipAddress, 45);

        if (!string.IsNullOrWhiteSpace(userAgent))
            UserAgent = TrimTo(userAgent, 256);
    }

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsValid(DateTime nowUtc, int maxAttempts = DefaultMaxAttempts)
        => !IsExpired(nowUtc) && !IsVerified && Attempts < maxAttempts;

    private static string? TrimTo(string? value, int max)
        => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}
