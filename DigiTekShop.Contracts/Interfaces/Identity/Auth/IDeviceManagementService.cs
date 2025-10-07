using DigiTekShop.Contracts.DTOs.Auth.UserDevice;
using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface IDeviceManagementService
    {
        Task<Result<IEnumerable<UserDeviceDto>>> GetUserDevicesAsync(string userId, CancellationToken ct = default);

        Task<Result> TrustDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default);

        Task<Result> UntrustDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default);

        Task<Result> RemoveDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default);

        Task<Result> CleanupInactiveDevicesAsync(string userId, CancellationToken ct = default);

        Task<Result<DeviceStatsDto>> GetDeviceStatsAsync(string userId, CancellationToken ct = default);
    }
}
