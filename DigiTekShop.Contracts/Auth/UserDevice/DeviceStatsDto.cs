namespace DigiTekShop.Contracts.Auth.UserDevice
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
