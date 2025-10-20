using DigiTekShop.Contracts.DTOs.Auth.Me;

namespace DigiTekShop.Contracts.Abstractions.Identity.Auth;
public interface IMeService
{
    Task<Result<MeResponse>> GetAsync(CancellationToken ct);
}