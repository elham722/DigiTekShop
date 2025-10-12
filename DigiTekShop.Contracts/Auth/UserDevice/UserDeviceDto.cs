namespace DigiTekShop.Contracts.Auth.UserDevice
{
    public sealed class UserDeviceDto
    {
        public Guid DeviceId { get; init; }

        public string DeviceName { get; init; } = default!;

        public string? DeviceFingerprint { get; init; }

        public string? BrowserInfo { get; init; }

        public string? OperatingSystem { get; init; }

        public string IpAddress { get; init; } = default!;

        public bool IsActive { get; init; }

        public bool IsTrusted { get; init; }

        public DateTime? TrustedAt { get; init; }

        public DateTime? TrustExpiresAt { get; init; }

        public DateTime LastLoginAt { get; init; }

        public bool IsTrustExpired => TrustExpiresAt.HasValue && DateTime.UtcNow >= TrustExpiresAt.Value;

        public bool IsCurrentlyTrusted => IsTrusted && !IsTrustExpired;
    }
}
