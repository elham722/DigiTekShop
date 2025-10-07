using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface IRegistrationService
    {
        Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);
        Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default);
        Task<Result> ResendEmailConfirmationAsync(ResendEmailConfirmationRequestDto request, CancellationToken ct = default);
    }
}
