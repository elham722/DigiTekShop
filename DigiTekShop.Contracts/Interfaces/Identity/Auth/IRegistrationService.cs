using DigiTekShop.Contracts.Auth.Register;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface IRegistrationService
    {
        Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);
    }
}
