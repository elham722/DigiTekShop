using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.Auth.Lockout;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Contracts.DTOs.Auth.PasswordHistory;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.DTOs.Auth.ResetPassword;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.DTOs.Auth.TwoFactor;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Identity.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly ILoginService _login;
    private readonly IRegistrationService _registration;
    private readonly IEmailConfirmationService _emailConf;
    private readonly IPasswordService _password;
    private readonly ITwoFactorService _twoFactor;
    private readonly ILockoutService _lockout;
    private readonly IPasswordHistoryService _pwdHistory;
    private readonly IPhoneVerificationService _phone;

    public AuthService(
        ILoginService login,
        IRegistrationService registration,
        IEmailConfirmationService emailConf,
        IPasswordService password,
        ITwoFactorService twoFactor,
        ILockoutService lockout,
        IPasswordHistoryService pwdHistory,
        IPhoneVerificationService phone)
    {
        _login = login;
        _registration = registration;
        _emailConf = emailConf;
        _password = password;
        _twoFactor = twoFactor;
        _lockout = lockout;
        _pwdHistory = pwdHistory;
        _phone = phone;
    }

    // Login / Refresh / Logout
    public Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
        => _login.LoginAsync(request, ct);

    public Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default)
        => _login.RefreshAsync(request, ct);

    public Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default)
        => _login.LogoutAsync(request, ct);

    public Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default)
        => _login.LogoutAllDevicesAsync(userId, ct);

    // Register / Email confirm
    public Task<Result> SendAsync(string userId, CancellationToken ct = default)
        => _emailConf.SendAsync(userId, ct);

    public Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default)
        => _emailConf.ConfirmEmailAsync(request, ct);

    public Task<Result> ResendAsync(ResendEmailConfirmationRequestDto request, CancellationToken ct = default)
        => _emailConf.ResendAsync(request, ct);

    public Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
        => _registration.RegisterAsync(request, ct);

    // Password
    public Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default)
        => _password.ForgotPasswordAsync(request, ct);

    public Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default)
        => _password.ResetPasswordAsync(request, ct);

    public Task<Result> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken ct = default)
        => _password.ChangePasswordAsync(request, ct);

    // 2FA
    public Task<Result<TwoFactorResponseDto>> EnableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default)
        => _twoFactor.EnableTwoFactorAsync(request, ct);

    public Task<Result<TwoFactorResponseDto>> DisableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default)
        => _twoFactor.DisableTwoFactorAsync(request, ct);

    public Task<Result> VerifyTwoFactorTokenAsync(VerifyTwoFactorRequestDto request, CancellationToken ct = default)
        => _twoFactor.VerifyTwoFactorTokenAsync(request, ct);

    public Task<Result<TwoFactorTokenResponseDto>> GenerateTwoFactorTokenAsync(TwoFactorRequestDto request, CancellationToken ct = default)
        => _twoFactor.GenerateTwoFactorTokenAsync(request, ct);

    // Lockout
    public Task<Result<LockUserResponseDto>> LockUserAsync(LockUserRequestDto request, CancellationToken ct = default)
        => _lockout.LockUserAsync(request, ct);

    public Task<Result<UnlockUserResponseDto>> UnlockUserAsync(UnlockUserRequestDto request, CancellationToken ct = default)
        => _lockout.UnlockUserAsync(request, ct);

    public Task<Result<LockoutStatusResponseDto>> GetLockoutStatusAsync(string userId, CancellationToken ct = default)
        => _lockout.GetLockoutStatusAsync(userId, ct);

    public Task<Result<TimeSpan?>> GetLockoutEndTimeAsync(string userId, CancellationToken ct = default)
        => _lockout.GetLockoutEndTimeAsync(userId, ct);

 
}
