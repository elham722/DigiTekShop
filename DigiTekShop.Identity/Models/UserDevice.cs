using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Models;

public class UserDevice
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;
    public string DeviceId { get; private set; } = default!;  
    public string DeviceName { get; private set; } = default!;
    public string? DeviceFingerprint { get; private set; }

    public DateTimeOffset FirstSeenUtc { get; private set; } // Set by DB via SYSUTCDATETIME()
    public DateTimeOffset LastSeenUtc { get; private set; } // Set by DB via SYSUTCDATETIME()
    public string? LastIp { get; private set; }
    public string? BrowserInfo { get; private set; }
    public string? OperatingSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset? TrustedAtUtc { get; private set; }
    public DateTimeOffset? TrustedUntilUtc { get; private set; }
    public int TrustCount { get; private set; }

    public byte[]? RowVersion { get; private set; }

    private UserDevice() { }

    public static UserDevice Create(
        Guid userId,
        string deviceId,
        string deviceName,
        DateTimeOffset nowUtc,
        string? ip = null,
        string? fingerprint = null,
        string? browser = null,
        string? os = null)
    {
        Guard.AgainstEmpty(userId, nameof(userId)); 
        Guard.AgainstNullOrEmpty(deviceId, nameof(deviceId)); 
        Guard.AgainstNullOrEmpty(deviceName, nameof(deviceName));

        return new UserDevice
        {
            UserId = userId,
            DeviceId = StringNormalizer.NormalizeAndTruncate(deviceId, 128)!,
            DeviceName = StringNormalizer.NormalizeAndTruncate(deviceName, 100)!,
            DeviceFingerprint = StringNormalizer.NormalizeAndTruncate(fingerprint, 256),
            // FirstSeenUtc and LastSeenUtc will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
            LastIp = StringNormalizer.NormalizeAndTruncate(ip, 45),
            BrowserInfo = StringNormalizer.NormalizeAndTruncate(browser, 512),
            OperatingSystem = StringNormalizer.NormalizeAndTruncate(os, 64),
            IsActive = true
        };
    }

    public void Touch(DateTimeOffset nowUtc, string? ip, string? browser, string? os)
    {
        LastSeenUtc = nowUtc;
        if (!string.IsNullOrWhiteSpace(ip)) LastIp = StringNormalizer.NormalizeAndTruncate(ip, 45);
        if (!string.IsNullOrWhiteSpace(browser)) BrowserInfo = StringNormalizer.NormalizeAndTruncate(browser, 512);
        if (!string.IsNullOrWhiteSpace(os)) OperatingSystem = StringNormalizer.NormalizeAndTruncate(os, 64);
        IsActive = true;
    }

    public void UpdateName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            DeviceName = StringNormalizer.NormalizeAndTruncate(name, 100)!;
        }
    }

    public void Untrust()
    {
        TrustedAtUtc = null;
        TrustedUntilUtc = null;
    }

    public void TrustUntil(DateTimeOffset untilUtc)
    {
        TrustUntil(untilUtc, DateTimeOffset.UtcNow);
    }

    public void TrustUntil(DateTimeOffset untilUtc, DateTimeOffset nowUtc)
    {
        if (untilUtc <= nowUtc)
            throw new ArgumentOutOfRangeException(nameof(untilUtc), "Trust until must be in the future.");

        TrustedAtUtc = nowUtc;
        TrustedUntilUtc = untilUtc;
        TrustCount++;
    }

    public void TrustFor(TimeSpan window, DateTimeOffset nowUtc)
    {
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window));
        TrustUntil(nowUtc + window, nowUtc);
    }

    public bool IsCurrentlyTrusted(DateTimeOffset nowUtc) 
        => TrustedUntilUtc.HasValue && TrustedUntilUtc.Value > nowUtc;

    public bool IsInactive(TimeSpan threshold, DateTimeOffset nowUtc)
        => !IsActive || nowUtc - LastSeenUtc > threshold;

    public void Deactivate() => IsActive = false;
}