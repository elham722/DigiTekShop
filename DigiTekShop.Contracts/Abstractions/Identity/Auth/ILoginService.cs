namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface ILoginService
    {
        Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
        Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default);
        Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default);
        Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default);
    }
}
