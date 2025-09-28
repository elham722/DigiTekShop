using System.Security.Cryptography;

namespace DigiTekShop.Identity.Models;
public class RefreshToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string TokenHash{ get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; private set; }
    public string? DeviceId { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    private RefreshToken() { }

    private RefreshToken(string tokenHash, DateTime expiresAt, Guid userId)
    {
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        UserId = userId;
    }

    public static RefreshToken Create(string tokenHash, DateTime expiresAt, Guid userId,
        string? deviceId = null, string? ip = null, string? userAgent = null)
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
            UserAgent = userAgent
        };
    }


    public bool Revoke(string? reason = null)
    {
        if (IsRevoked) return false;
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        return true;
    }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

}