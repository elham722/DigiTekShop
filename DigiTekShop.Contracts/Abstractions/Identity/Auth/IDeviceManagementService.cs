namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface IDeviceManagementService
    {
        Task<Result<IEnumerable<UserDeviceDto>>> GetUserDevicesAsync(string userId, CancellationToken ct = default);

        Task<Result> TrustDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default);

        Task<Result> TrustDeviceUntilAsync(string userId, Guid deviceId, DateTime expiresAt, CancellationToken ct = default);

        Task<Result> TrustDeviceForAsync(string userId, Guid deviceId, TimeSpan duration, CancellationToken ct = default);

        Task<Result> UntrustDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default);

        Task<Result> RemoveDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default);

        Task<Result> CleanupInactiveDevicesAsync(string userId, CancellationToken ct = default);

        Task<Result<DeviceStatsDto>> GetDeviceStatsAsync(string userId, CancellationToken ct = default);
    }
}
