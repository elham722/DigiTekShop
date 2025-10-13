using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Security
{
    public sealed class DeviceLimitsOptions
    {
        public int MaxActiveDevicesPerUser { get; init; } = 5;
        public int MaxTrustedDevicesPerUser { get; init; } = 3;
        public TimeSpan DeviceInactivityThreshold { get; init; } = TimeSpan.FromDays(30);
        public int MaxTrustAttempts { get; init; } = 3;
        public TimeSpan DeviceTokenExpiration { get; init; } = TimeSpan.FromDays(90);
        public bool AutoDeactivateInactiveDevices { get; init; } = true;
        public bool DefaultTrustNewDevices { get; init; }
    }
}
