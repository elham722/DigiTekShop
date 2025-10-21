using DigiTekShop.Contracts.DTOs.Auth.Mfa;

namespace DigiTekShop.Contracts.Abstractions.Identity.Mfa;
public interface IMfaService
{
    Task<Result<LoginResponse>> VerifyAsync(VerifyMfaRequest dto, CancellationToken ct);
}
