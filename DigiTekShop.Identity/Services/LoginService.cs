using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Identity.Services
{
    public class LoginService : ILoginService
    {
        public Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
