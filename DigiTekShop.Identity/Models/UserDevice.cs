namespace DigiTekShop.Identity.Models
{
    public class UserDevice
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string DeviceName { get; private set; } = default!;
        public string IpAddress { get; private set; } = default!;
        public DateTime LastLoginAt { get; private set; } = DateTime.UtcNow;
        public bool IsActive { get; private set; } = true;

        // پیشنهاد جدید
        public string? DeviceFingerprint { get; private set; }
        public string? BrowserInfo { get; private set; }
        public string? OperatingSystem { get; private set; }
        public bool IsTrusted { get; private set; }
        public DateTime? TrustedAt { get; private set; }

        // User
        public Guid UserId { get; private set; }
        public User User { get; private set; } = default!;

        private UserDevice() { }

        public static UserDevice Create(
            Guid userId,
            string deviceName,
            string ipAddress,
            string? fingerprint = null,
            string? browser = null,
            string? os = null)
        {
            Guard.AgainstEmpty(userId, nameof(userId));
            Guard.AgainstNullOrEmpty(deviceName, nameof(deviceName));
            Guard.AgainstNullOrEmpty(ipAddress, nameof(ipAddress));

            return new UserDevice
            {
                UserId = userId,
                DeviceName = deviceName,
                IpAddress = ipAddress,
                DeviceFingerprint = fingerprint,
                BrowserInfo = browser,
                OperatingSystem = os
            };
        }

        public void Deactivate() => IsActive = false;

        public void UpdateLogin(DateTime loginTime)
        {
            Guard.AgainstPastDate(loginTime, () => DateTime.UtcNow, nameof(loginTime));
            LastLoginAt = loginTime;
            IsActive = true;
        }

        public void MarkAsTrusted()
        {
            IsTrusted = true;
            TrustedAt = DateTime.UtcNow;
        }

        public void MarkAsUntrusted()
        {
            IsTrusted = false;
            TrustedAt = null;
        }

       
        public bool RequiresReVerification(TimeSpan trustExpirationThreshold)
        {
            if (!IsTrusted || !TrustedAt.HasValue)
                return true;

            return DateTime.UtcNow - TrustedAt.Value > trustExpirationThreshold;
        }

       
        public bool IsInactive(TimeSpan inactivityThreshold)
        {
            return !IsActive || DateTime.UtcNow - LastLoginAt > inactivityThreshold;
        }

      
        public void UpdateDeviceInfo(string? deviceName = null, string? browserInfo = null, string? operatingSystem = null)
        {
            if (!string.IsNullOrWhiteSpace(deviceName))
                DeviceName = deviceName;
            
            if (!string.IsNullOrWhiteSpace(browserInfo))
                BrowserInfo = browserInfo;
            
            if (!string.IsNullOrWhiteSpace(operatingSystem))
                OperatingSystem = operatingSystem;
        }
    }
}
