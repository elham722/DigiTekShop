namespace DigiTekShop.Contracts.Auth.UserDevice
{
    public sealed record UserDeviceDto(
        Guid DeviceId,
        string DeviceName,
        string? DeviceFingerprint,
        string? BrowserInfo,
        string? OperatingSystem,
        string IpAddress,
        bool IsActive,
        bool IsTrusted,
        DateTime? TrustedAt,
        DateTime? TrustExpiresAt,
        DateTime LastLoginAt
    )
    {
        public bool IsTrustExpired => TrustExpiresAt.HasValue && DateTime.UtcNow >= TrustExpiresAt.Value;

        public bool IsCurrentlyTrusted => IsTrusted && !IsTrustExpired;
    }
}
