
using System.ComponentModel.DataAnnotations;
using DigiTekShop.SharedKernel;

namespace DigiTekShop.Identity.Models;

public sealed class RefreshToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required]
    public string TokenHash { get; private set; } = default!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastUsedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public DateTimeOffset? RotatedAtUtc { get; private set; }

    public string? RevokedReason { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? ParentTokenHash { get; private set; }
    public int UsageCount { get; private set; }

    public string? CreatedByIp { get; private set; }
    public string? DeviceId { get; private set; }
    public string? UserAgent { get; private set; }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    [Timestamp]
    public byte[] RowVersion { get; private set; } = default!;

    private RefreshToken() { }

    public static RefreshToken Create(
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        Guid userId,
        string? deviceId = null,
        string? createdByIp = null,
        string? userAgent = null,
        string? parentTokenHash = null,
        DateTimeOffset? createdAtUtc = null)
    {
        Guard.AgainstNullOrEmpty(tokenHash,nameof(tokenHash));
        if (expiresAtUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "ExpiresAtUtc must be in the future.");

        return new RefreshToken
        {
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow,
            UserId = userId,
            DeviceId = deviceId,
            CreatedByIp = createdByIp,
            UserAgent = userAgent,
            ParentTokenHash = parentTokenHash
        };
    }

    public bool Revoke(string? reason = null, DateTimeOffset? now = null)
    {
        if (IsRevoked) return false;
        RevokedAtUtc = now ?? DateTimeOffset.UtcNow;
        RevokedReason = reason;
        return true;
    }

    public void MarkAsRotated(string newTokenHash, DateTimeOffset? now = null)
    {
        RotatedAtUtc = now ?? DateTimeOffset.UtcNow;
        ReplacedByTokenHash = newTokenHash;
        if (RevokedAtUtc is null)
        {
            RevokedAtUtc = RotatedAtUtc;
            RevokedReason = "rotated";
        }
    }

    public void MarkAsUsed(DateTimeOffset? now = null)
    {
        UsageCount++;
        LastUsedAtUtc = now ?? DateTimeOffset.UtcNow;
    }

    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsActive => !IsRevoked && !IsExpired;
}
