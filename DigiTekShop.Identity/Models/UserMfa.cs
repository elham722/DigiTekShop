namespace DigiTekShop.Identity.Models
{
    public class UserMfa
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid UserId { get; private set; }
        public User User { get; private set; } = default!;
        public string SecretKeyEncrypted { get; private set; } = string.Empty;
        public bool IsEnabled { get; private set; } = false;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public int Attempts { get; private set; } = 0;
        public DateTime? LastVerifiedAt { get; private set; }
        
        // Lock fields
        public bool IsLocked { get; private set; } = false;
        public DateTime? LockedAt { get; private set; }
        public DateTime? LockedUntil { get; private set; }

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
            LastVerifiedAt = DateTime.UtcNow;
        }

      
        public void LockMfa(TimeSpan duration)
        {
            IsLocked = true;
            LockedAt = DateTime.UtcNow;
            LockedUntil = DateTime.UtcNow.Add(duration);
        }

        public void LockMfaUntil(DateTime until)
        {
            Guard.AgainstPastDate(until, () => DateTime.UtcNow, nameof(until));
            
            IsLocked = true;
            LockedAt = DateTime.UtcNow;
            LockedUntil = until;
        }

        public void UnlockMfa()
        {
            IsLocked = false;
            LockedAt = null;
            LockedUntil = null;
            ResetAttempts();
        }

      
        public bool IsCurrentlyLocked => IsLocked && LockedUntil.HasValue && DateTime.UtcNow < LockedUntil.Value;

        public bool IsLockExpired => IsLocked && LockedUntil.HasValue && DateTime.UtcNow >= LockedUntil.Value;
    }
}