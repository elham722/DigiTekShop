namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface ILoginService
    {
        Task<Result<LoginResponse>> LoginAsync(LoginRequest dto, CancellationToken ct);
    }
}
