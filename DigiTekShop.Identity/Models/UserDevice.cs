using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Models
{
    public class UserDevice
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string DeviceName { get; private set; } = default!;
        public string IpAddress { get; private set; } = default!;
        public DateTime LastLoginAt { get; private set; } = DateTime.UtcNow;
        public bool IsActive { get; private set; } = true;

        // ارتباط با User
        public string UserId { get; private set; } = default!;
        public User User { get; private set; } = default!;

        private UserDevice() { } // EF Core
        public UserDevice(string deviceName, string ipAddress, string userId)
        {
            DeviceName = deviceName;
            IpAddress = ipAddress;
            UserId = userId;
        }

        public void Deactivate() => IsActive = false;
        public void UpdateLogin(DateTime loginTime) => LastLoginAt = loginTime;
    }
}
