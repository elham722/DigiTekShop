using DigiTekShop.Contracts.Auth.Login;
using DigiTekShop.Contracts.Auth.Logout;
using DigiTekShop.Contracts.Auth.Token;
using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface ILoginService
    {
        Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
        Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default);
        Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default);
        Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default);
    }
}
