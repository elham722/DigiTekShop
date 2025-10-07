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

    // Throttle fields
    public int AttemptCount { get; private set; } = 0;
    public DateTime? LastAttemptAt { get; private set; }
    public DateTime? ThrottleUntil { get; private set; }

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

   
    public void RecordFailedAttempt(int maxAttempts = 3, TimeSpan throttleDuration = default)
    {
        if (IsUsed || IsExpired) return;

        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;

        if (AttemptCount >= maxAttempts)
        {
            ThrottleUntil = DateTime.UtcNow.Add(throttleDuration == default ? TimeSpan.FromMinutes(15) : throttleDuration);
        }
    }

    public void ClearThrottle()
    {
        ThrottleUntil = null;
        AttemptCount = 0;
        LastAttemptAt = null;
    }

  
    public bool IsThrottled => ThrottleUntil.HasValue && DateTime.UtcNow < ThrottleUntil.Value;

    public bool IsValid => !IsUsed && !IsExpired && !IsThrottled;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}