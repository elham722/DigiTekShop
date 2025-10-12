namespace DigiTekShop.Contracts.Abstractions.Identity.Lockout
{
    public interface ILockoutService
    {
        Task<Result<LockUserResponseDto>> LockUserAsync(LockUserRequestDto request, CancellationToken ct = default);
        Task<Result<UnlockUserResponseDto>> UnlockUserAsync(UnlockUserRequestDto request, CancellationToken ct = default);
        Task<Result<LockoutStatusResponseDto>> GetLockoutStatusAsync(string userId, CancellationToken ct = default);
        Task<Result<TimeSpan?>> GetLockoutEndTimeAsync(string userId, CancellationToken ct = default);
    }
}
