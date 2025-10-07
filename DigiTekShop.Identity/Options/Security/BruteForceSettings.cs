using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Options.Security
{
    public class BruteForceSettings
    {
        public bool Enabled { get; init; } = true;

        public int MaxFailedAttempts { get; init; } = 5;

        public TimeSpan TimeWindow { get; init; } = TimeSpan.FromMinutes(15);

        public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(30);

        public bool IpBasedLockout { get; init; } = true;

        public bool DeviceBasedLockout { get; init; } = true;

        public int MaxFailedAttemptsPerIp { get; init; } = 10;

        public int MaxFailedAttemptsPerDevice { get; init; } = 8;
    }

}
