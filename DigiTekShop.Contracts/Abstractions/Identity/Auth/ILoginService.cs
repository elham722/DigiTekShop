namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface ILoginService
    {
        Task<Result<RefreshTokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
        Task<Result<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
        Task<Result> LogoutAsync(LogoutRequest request, CancellationToken ct = default);
        Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default);
    }
}
