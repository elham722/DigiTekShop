namespace DigiTekShop.Contracts.Abstractions.Identity.Auth;
public interface IMfaService
{
    Task<Result<LoginResponse>> VerifyAsync(VerifyMfaRequest dto, CancellationToken ct);
}
