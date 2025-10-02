namespace DigiTekShop.Identity.Models;

/// <summary>
/// Entity for tracking password reset tokens and their expiration
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; } = false;
    public DateTime? UsedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    // Navigation property
    public User User { get; private set; } = default!;

    private PasswordResetToken() { }

    public static PasswordResetToken Create(
        Guid userId, 
        string tokenHash, 
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(tokenHash, nameof(tokenHash));
        Guard.AgainstPastDate(expiresAt, () => DateTime.UtcNow, nameof(expiresAt));

        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public void MarkAsUsed(string? ipAddress = null)
    {
        if (IsUsed) return;
        
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(ipAddress))
            IpAddress = ipAddress;
    }

    public bool IsValid => !IsUsed && !IsExpired;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}