using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface IPermissionEvaluatorService
    {
        Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default);

        Task<Result<IEnumerable<string>>> GetEffectivePermissionsAsync(string userId, CancellationToken ct = default);

        Task<Result<Dictionary<string, bool>>> CheckMultiplePermissionsAsync(string userId, IEnumerable<string> permissionNames, CancellationToken ct = default);

        Task<Result<IEnumerable<string>>> GetRolePermissionsAsync(string userId, CancellationToken ct = default);

        Task<Result<IEnumerable<string>>> GetDirectPermissionsAsync(string userId, CancellationToken ct = default);
    }

}
