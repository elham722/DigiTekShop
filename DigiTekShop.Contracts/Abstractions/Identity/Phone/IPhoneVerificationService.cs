namespace DigiTekShop.Contracts.Abstractions.Identity.Phone
{
    public interface IPhoneVerificationService
    {
        Task<Result> SendVerificationCodeAsync(Guid userId, string phoneNumber, CancellationToken ct = default);
        Task<Result> VerifyCodeAsync(string userId, string code, CancellationToken ct = default);
        Task<bool> CanResendCodeAsync(Guid userId, CancellationToken ct = default);
    }
}
