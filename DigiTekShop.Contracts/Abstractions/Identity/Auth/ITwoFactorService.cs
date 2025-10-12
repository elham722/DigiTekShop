namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface ITwoFactorService
    {
        Task<Result<TwoFactorResponseDto>> EnableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default);
        Task<Result<TwoFactorResponseDto>> DisableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default);
        Task<Result> VerifyTwoFactorTokenAsync(VerifyTwoFactorRequestDto request, CancellationToken ct = default);
        Task<Result<TwoFactorTokenResponseDto>> GenerateTwoFactorTokenAsync(TwoFactorRequestDto request, CancellationToken ct = default);
    }
}
