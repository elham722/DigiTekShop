using System.Security.Claims;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Interfaces.Identity;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Exceptions.Common; 
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Identity.Services;

public sealed class LoginService : ILoginService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginService> _logger;

    public LoginService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtTokenService jwtTokenService,
        ILogger<LoginService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<TokenResponseDto>.Failure("Email and password are required.");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_CREDENTIALS), IdentityErrorCodes.INVALID_CREDENTIALS);

        
         if (user.IsDeleted) return Result<TokenResponseDto>.Failure("User not found or inactive.");

        if (await _userManager.IsLockedOutAsync(user))
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.ACCOUNT_LOCKED), IdentityErrorCodes.ACCOUNT_LOCKED);

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signIn.Succeeded)
        {
            
            if (await _userManager.GetTwoFactorEnabledAsync(user))
                return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.REQUIRES_TWO_FACTOR), IdentityErrorCodes.REQUIRES_TWO_FACTOR);

           
            var tokens = await _jwtTokenService.GenerateTokensAsync(
                userId: user.Id.ToString(),
                deviceId: request.DeviceId,
                ipAddress: request.Ip,
                userAgent: request.UserAgent,
                ct: ct);

            return tokens;
        }

        if (signIn.IsLockedOut)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.ACCOUNT_LOCKED), IdentityErrorCodes.ACCOUNT_LOCKED);

        if (signIn.RequiresTwoFactor)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.REQUIRES_TWO_FACTOR), IdentityErrorCodes.REQUIRES_TWO_FACTOR);

        if (signIn.IsNotAllowed)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.SIGNIN_NOT_ALLOWED), IdentityErrorCodes.SIGNIN_NOT_ALLOWED);

        // invalid credentials
        return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_CREDENTIALS), IdentityErrorCodes.INVALID_CREDENTIALS);
    }

    public async Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN), IdentityErrorCodes.INVALID_TOKEN);

        var result = await _jwtTokenService.RefreshTokensAsync(
            refreshToken: request.RefreshToken,
            deviceId: request.DeviceId,
            ipAddress: request.Ip,
            userAgent: request.UserAgent,
            ct: ct);

        return result;
    }

    public async Task<Result> LogoutAsync(LogoutRequestDto request, CancellationToken ct = default)
    {
       
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result.Success(); 

        var res = await _jwtTokenService.RevokeRefreshTokenAsync(request.RefreshToken, reason: "User logout", ct);
        return res;
    }

    public async Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure("User id is required.");

        var res = await _jwtTokenService.RevokeAllUserTokensAsync(userId, reason: "User logout all devices", ct);
        return res;
    }
}
