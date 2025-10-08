using System;
using System.Security.Cryptography;

namespace DigiTekShop.Identity.Models
{
    public class RefreshToken
    {
        // Primary Key
        public Guid Id { get; private set; } = Guid.NewGuid();

        // Hashed Token
        public string TokenHash { get; private set; } = null!;

        // Expiration
        public DateTime ExpiresAt { get; private set; }

        // Revocation
        public bool IsRevoked { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public string? RevokedReason { get; private set; }
        public string? ReplacedByTokenHash { get; private set; }

        // Rotation
        public bool IsRotated { get; private set; }
        public DateTime? RotatedAt { get; private set; }
        public string? ParentTokenHash { get; private set; }

        // Usage Tracking
        public int UsageCount { get; private set; }
        public DateTime? LastUsedAt { get; private set; }

        // Metadata
        public DateTime CreatedAt { get; private set; } 
        public string? CreatedByIp { get; private set; }
        public string? DeviceId { get; private set; }
        public string? UserAgent { get; private set; }

        // User
        public Guid UserId { get; private set; }
        public User User { get; private set; } = default!;

        // ✅ Optimistic Concurrency Control (prevents race conditions in token rotation)
        public byte[] RowVersion { get; private set; } = default!;

        // Private constructor for EF Core
        private RefreshToken() { }

        // Factory method
        public static RefreshToken Create(
            string tokenHash,
            DateTime expiresAt,
            Guid userId,
            string? deviceId = null,
            string? ip = null,
            string? userAgent = null,
            string? parentTokenHash = null)
        {
            Guard.AgainstNullOrEmpty(tokenHash, nameof(tokenHash));
            Guard.AgainstPastDate(expiresAt, () => DateTime.UtcNow, nameof(expiresAt));

            return new RefreshToken
            {
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                UserId = userId,
                DeviceId = deviceId,
                CreatedByIp = ip,
                UserAgent = userAgent,
                ParentTokenHash = parentTokenHash
            };
        }

        // Mark token as revoked
        public bool Revoke(string? reason = null)
        {
            if (IsRevoked) return false;
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            RevokedReason = reason;
            return true;
        }

        // Mark token as rotated
        public void MarkAsRotated(string newTokenHash)
        {
            IsRotated = true;
            RotatedAt = DateTime.UtcNow;
            ReplacedByTokenHash = newTokenHash;
        }

        // Update usage stats
        public void MarkAsUsed()
        {
            UsageCount++;
            LastUsedAt = DateTime.UtcNow;
        }

        // Helper properties
        public bool IsActive => !IsRevoked && !IsExpired;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
