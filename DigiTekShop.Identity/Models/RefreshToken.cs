using DigiTekShop.SharedKernel.Guards;

namespace DigiTekShop.Identity.Models
{
    public class RefreshToken
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Token { get; private set; } = default!;
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; private set; }
        public string? RevokedReason { get; private set; }

        public Guid UserId { get; private set; }
        public User User { get; private set; } = default!;

        private RefreshToken() { }

        private RefreshToken(string token, DateTime expiresAt, Guid userId)
        {
            Token = token;
            ExpiresAt = expiresAt;
            UserId = userId;
        }

        public static RefreshToken Create(string token, DateTime expiresAt, Guid userId)
        {
           Guard.AgainstNullOrEmpty(token,nameof(token));
           Guard.AgainstPastDate(expiresAt, () => DateTime.UtcNow, nameof(expiresAt));

            return new RefreshToken(token, expiresAt, userId);
        }

        public void Revoke(string? reason = null)
        {
            if (IsRevoked) return;
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            RevokedReason = reason;
        }

        public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}