using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.Auth.Lockout;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.DTOs.Auth.ResetPassword;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.DTOs.Auth.TwoFactor;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth;

public interface IAuthService
{
    // Login / Refresh / Logout
    Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default);
    Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default);
    Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default);

    // Register / Email confirm
    Task<Result> SendAsync(string userId, CancellationToken ct = default);
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default);
    Task<Result> ResendAsync(ResendEmailConfirmationRequestDto request, CancellationToken ct = default);

    // Password
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken ct = default);

    // 2FA
    Task<Result<TwoFactorResponseDto>> EnableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default);
    Task<Result<TwoFactorResponseDto>> DisableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default);
    Task<Result> VerifyTwoFactorTokenAsync(VerifyTwoFactorRequestDto request, CancellationToken ct = default);
    Task<Result<TwoFactorTokenResponseDto>> GenerateTwoFactorTokenAsync(TwoFactorRequestDto request, CancellationToken ct = default);

    // Lockout
    Task<Result<LockUserResponseDto>> LockUserAsync(LockUserRequestDto request, CancellationToken ct = default);
    Task<Result<UnlockUserResponseDto>> UnlockUserAsync(UnlockUserRequestDto request, CancellationToken ct = default);
    Task<Result<LockoutStatusResponseDto>> GetLockoutStatusAsync(string userId, CancellationToken ct = default);
    Task<Result<TimeSpan?>> GetLockoutEndTimeAsync(string userId, CancellationToken ct = default);
}