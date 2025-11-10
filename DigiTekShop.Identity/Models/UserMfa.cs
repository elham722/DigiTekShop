using DigiTekShop.SharedKernel.Guards;

namespace DigiTekShop.Identity.Models;

public class UserMfa
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;
    public string SecretKeyEncrypted { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; } = false;
    public DateTimeOffset CreatedAt { get; private set; } // Set by DB via SYSUTCDATETIME()
    public int Attempts { get; private set; } = 0;
    public DateTimeOffset? LastVerifiedAt { get; private set; }
    
    // Lock fields
    public bool IsLocked { get; private set; } = false;
    public DateTimeOffset? LockedAt { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    private UserMfa() { }

    public static UserMfa Create(Guid userId, string secretKey)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(secretKey, nameof(secretKey));

        return new UserMfa
        {
            UserId = userId,
            SecretKeyEncrypted = secretKey,
            IsEnabled = true,
            Attempts = 0
            // CreatedAt will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
        };
    }

    public void Enable(string secretKeyEncrypted)
    {
        Guard.AgainstNullOrEmpty(secretKeyEncrypted, nameof(secretKeyEncrypted));
        SecretKeyEncrypted = secretKeyEncrypted;
        IsEnabled = true;
        Attempts = 0; 
    }

    public void Disable()
    {
        if (!IsEnabled) return;
        IsEnabled = false;
        ResetAttempts();
    }

    public void IncrementAttempts(int maxAttempts, TimeSpan lockDuration = default)
    {
        Guard.AgainstNegative(maxAttempts, nameof(maxAttempts));

        // Ensure lock freshness before incrementing
        EnsureLockFreshness(DateTimeOffset.UtcNow);

        Attempts++;

        if (Attempts >= maxAttempts)
        {
            LockMfa(lockDuration == default ? TimeSpan.FromMinutes(15) : lockDuration);
            throw new InvalidOperationException(
                $"Maximum MFA attempts ({maxAttempts}) exceeded for user {UserId}. MFA locked.");
        }
    }

    public void ResetAttempts() => Attempts = 0;

    public void MarkVerified()
    {
        ResetAttempts();
        LastVerifiedAt = DateTimeOffset.UtcNow;
    }

    public void LockMfa(TimeSpan duration)
    {
        var now = DateTimeOffset.UtcNow;
        IsLocked = true;
        LockedAt = now;
        LockedUntil = now.Add(duration);
    }

    public void LockMfaUntil(DateTimeOffset until)
    {
        Guard.AgainstPastDate(until, () => DateTimeOffset.UtcNow, nameof(until));
        
        var now = DateTimeOffset.UtcNow;
        IsLocked = true;
        LockedAt = now;
        LockedUntil = until;
    }

    public void UnlockMfa()
    {
        IsLocked = false;
        LockedAt = null;
        LockedUntil = null;
        ResetAttempts();
    }

    /// <summary>
    /// Ensures lock freshness by unlocking if the lock has expired
    /// </summary>
    public void EnsureLockFreshness(DateTimeOffset now)
    {
        if (IsLocked && LockedUntil.HasValue && now >= LockedUntil.Value)
        {
            UnlockMfa();
        }
    }

    public bool IsCurrentlyLocked(DateTimeOffset now) 
        => IsLocked && LockedUntil.HasValue && now < LockedUntil.Value;

    public bool IsLockExpired(DateTimeOffset now) 
        => IsLocked && LockedUntil.HasValue && now >= LockedUntil.Value;
}