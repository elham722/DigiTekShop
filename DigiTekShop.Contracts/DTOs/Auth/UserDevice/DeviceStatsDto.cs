using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.UserDevice
{
    public class DeviceStatsDto
    {
        public int TotalDevices { get; set; }
        public int ActiveDevices { get; set; }
        public int TrustedDevices { get; set; }
        public int MaxActiveDevices { get; set; }
        public int MaxTrustedDevices { get; set; }
        public DateTime LastCleanupAt { get; set; }
    }
}
