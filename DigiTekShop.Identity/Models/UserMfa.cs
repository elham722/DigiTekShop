namespace DigiTekShop.Identity.Models
{
    public class UserMfa
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid UserId { get; private set; }
        public string SecretKeyEncrypted { get; private set; } = string.Empty;
        public bool IsEnabled { get; private set; } = false;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public int Attempts { get; private set; } = 0;
        public DateTime? LastVerifiedAt { get; private set; }

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
            Attempts = 0; // ریست بشه
        }

        public void Disable()
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            ResetAttempts();
        }
        public void IncrementAttempts(int maxAttempts)
        {
            Guard.AgainstNegative(maxAttempts, nameof(maxAttempts));

            if (Attempts >= maxAttempts)
                throw new InvalidOperationException(
                    $"Maximum MFA attempts ({maxAttempts}) exceeded for user {UserId}");

            Attempts++;
        }

        public void ResetAttempts() => Attempts = 0;

        public void MarkVerified()
        {
            ResetAttempts();
            LastVerifiedAt = DateTime.UtcNow;
        }
    }
}