using DigiTekShop.Contracts.DTOs.Auth.ResetPassword;
using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface IPasswordService
    {
        Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default);
        Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);
        Task<Result> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken ct = default);
    }
}
