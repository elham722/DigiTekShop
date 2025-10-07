using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface IPhoneVerificationService
    {
        Task<Result> SendVerificationCodeAsync(Guid userId, string phoneNumber, CancellationToken ct = default);
        Task<Result> VerifyCodeAsync(string userId, string code, CancellationToken ct = default);
        Task<bool> CanResendCodeAsync(Guid userId, CancellationToken ct = default);
    }
}
