using DigiTekShop.Contracts.DTOs.Auth.Lockout;
using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface ILockoutService
    {
        Task<Result<LockUserResponseDto>> LockUserAsync(LockUserRequestDto request, CancellationToken ct = default);
        Task<Result<UnlockUserResponseDto>> UnlockUserAsync(UnlockUserRequestDto request, CancellationToken ct = default);
        Task<Result<LockoutStatusResponseDto>> GetLockoutStatusAsync(string userId, CancellationToken ct = default);
        Task<Result<TimeSpan?>> GetLockoutEndTimeAsync(string userId, CancellationToken ct = default);
    }
}
