namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface ILoginService
    {
        Task<Result<LoginResultDto>> LoginAsync(LoginRequest dto, CancellationToken ct);
    }
}
