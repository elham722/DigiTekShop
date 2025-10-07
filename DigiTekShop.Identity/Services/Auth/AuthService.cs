using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.Auth.Lockout;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.DTOs.Auth.ResetPassword;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.DTOs.Auth.TwoFactor;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using static DigiTekShop.Identity.Services.Auth.AuthService;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DigiTekShop.Identity.Services.Auth;

public sealed class AuthService : IAuthService
{

    private readonly ILoginService _login;
    private readonly IRegistrationService _registration;
    private readonly IPasswordService _passwords;
    private readonly ITwoFactorService _twoFactor;
    private readonly ILockoutService _lockout;
    private readonly IEmailConfirmationService _emailConf;

    public AuthService(
        ILoginService login, IRegistrationService registration, IPasswordService passwords,
        ITwoFactorService twoFactor, ILockoutService lockout, IEmailConfirmationService emailConf)
    {
        _login = login; _registration = registration; _passwords = passwords;
        _twoFactor = twoFactor; _lockout = lockout;
        _emailConf = emailConf;
    }

    public Task<Result> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }


    public Task<Result<TwoFactorResponseDto>> DisableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TwoFactorResponseDto>> EnableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TwoFactorTokenResponseDto>> GenerateTwoFactorTokenAsync(TwoFactorRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TimeSpan?>> GetLockoutEndTimeAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<LockoutStatusResponseDto>> GetLockoutStatusAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<LockUserResponseDto>> LockUserAsync(LockUserRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

   

    public Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<UnlockUserResponseDto>> UnlockUserAsync(UnlockUserRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> VerifyTwoFactorTokenAsync(VerifyTwoFactorRequestDto request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto req, CancellationToken ct = default)
        => _emailConf.ConfirmEmailAsync(req, ct);

    public Task<Result> ResendAsync(ResendEmailConfirmationRequestDto req, CancellationToken ct = default)
        => _emailConf.ResendAsync(req, ct);

   
    public Task<Result> SendAsync(string userId, CancellationToken ct = default)
        => _emailConf.SendAsync(userId, ct);


  
}
