namespace DigiTekShop.Contracts.DTOs.Auth.UserDevice
{
    public record DeviceStatsDto(
        int TotalDevices,
        int ActiveDevices,
        int TrustedDevices,
        int MaxActiveDevices,
        int MaxTrustedDevices,
        DateTime LastCleanupAt
    );
}
