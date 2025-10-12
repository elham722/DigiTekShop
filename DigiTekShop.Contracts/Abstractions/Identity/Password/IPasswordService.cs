namespace DigiTekShop.Contracts.Abstractions.Identity.Password
{
    public interface IPasswordService
    {
        Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default);
        Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);
        Task<Result> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken ct = default);
    }
}
