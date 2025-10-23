namespace DigiTekShop.Identity.Models;

public class UserDevice
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;
    public string DeviceId { get; private set; } = default!;  
    public string DeviceName { get; private set; } = default!;
    public string? DeviceFingerprint { get; private set; }

    public DateTime FirstSeenUtc { get; private set; }
    public DateTime LastSeenUtc { get; private set; }
    public string? LastIp { get; private set; }
    public string? BrowserInfo { get; private set; }
    public string? OperatingSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset? TrustedAtUtc { get; private set; }
    public DateTimeOffset? TrustedUntilUtc { get; private set; }
    public int TrustCount { get; private set; }

    private UserDevice() { }

    public static UserDevice Create(
        Guid userId,
        string deviceId,
        string deviceName,
        DateTime nowUtc,
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
            DeviceId = deviceId,
            DeviceName = deviceName,
            DeviceFingerprint = string.IsNullOrWhiteSpace(fingerprint) ? null : fingerprint,
            FirstSeenUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            LastSeenUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            LastIp = string.IsNullOrWhiteSpace(ip) ? null : ip,
            BrowserInfo = string.IsNullOrWhiteSpace(browser) ? null : browser,
            OperatingSystem = string.IsNullOrWhiteSpace(os) ? null : os,
            IsActive = true
        };
    }

    public void Touch(DateTime nowUtc, string? ip, string? browser, string? os)
    {
        LastSeenUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        if (!string.IsNullOrWhiteSpace(ip)) LastIp = ip;
        if (!string.IsNullOrWhiteSpace(browser)) BrowserInfo = browser;
        if (!string.IsNullOrWhiteSpace(os)) OperatingSystem = os;
        IsActive = true;
    }

    public void UpdateName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name)) DeviceName = name;
    }

    public void Untrust()
    {
        TrustedAtUtc = null;
        TrustedUntilUtc = null;
    }

    public void TrustUntil(DateTimeOffset untilUtc)
    {
        if (untilUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(untilUtc), "Trust until must be in the future.");

        TrustedAtUtc = DateTimeOffset.UtcNow;
        TrustedUntilUtc = untilUtc;
        TrustCount++;
    }

    public void TrustFor(TimeSpan window, DateTimeOffset nowUtc)
    {
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window));
        TrustUntil(nowUtc + window);
    }

    public bool IsCurrentlyTrusted(DateTimeOffset nowUtc) 
        => TrustedUntilUtc.HasValue && TrustedUntilUtc.Value > nowUtc;

    public bool IsInactive(TimeSpan threshold, DateTime nowUtc)
        => !IsActive || nowUtc - LastSeenUtc > threshold;

    public void Deactivate() => IsActive = false;
}