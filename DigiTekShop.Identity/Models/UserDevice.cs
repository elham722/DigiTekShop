namespace DigiTekShop.Identity.Models;
    public class UserDevice
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string DeviceName { get; private set; } = default!;
        public string IpAddress { get; private set; } = default!;
        public DateTime LastLoginAt { get; private set; } = DateTime.UtcNow;
        public bool IsActive { get; private set; } = true;

        public Guid UserId { get; private set; }
        public User User { get; private set; } = default!;

        private UserDevice() { }

        public static UserDevice Create(Guid userId, string deviceName, string ipAddress)
        {
            Guard.AgainstEmpty(userId, nameof(userId));
            Guard.AgainstNullOrEmpty(deviceName, nameof(deviceName));
            Guard.AgainstNullOrEmpty(ipAddress, nameof(ipAddress));

            return new UserDevice
            {
                UserId = userId,
                DeviceName = deviceName,
                IpAddress = ipAddress
            };
        }

        public void Deactivate() => IsActive = false;

        public void UpdateLogin(DateTime loginTime)
        {
            Guard.AgainstPastDate(loginTime, () => DateTime.UtcNow, nameof(loginTime));
            LastLoginAt = loginTime;
            IsActive = true; 
        }
    }