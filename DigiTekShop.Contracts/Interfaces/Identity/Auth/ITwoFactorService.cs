using DigiTekShop.Contracts.DTOs.Auth.TwoFactor;
using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface ITwoFactorService
    {
        Task<Result<TwoFactorResponseDto>> EnableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default);
        Task<Result<TwoFactorResponseDto>> DisableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default);
        Task<Result> VerifyTwoFactorTokenAsync(VerifyTwoFactorRequestDto request, CancellationToken ct = default);
        Task<Result<TwoFactorTokenResponseDto>> GenerateTwoFactorTokenAsync(TwoFactorRequestDto request, CancellationToken ct = default);
    }
}
