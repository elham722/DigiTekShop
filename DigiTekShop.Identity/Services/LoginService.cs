using System.Security.Claims;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Interfaces.Identity;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Exceptions.Common;
using DigiTekShop.Identity.Models;
using DigiTekShop.Identity.Options.Security;
using DigiTekShop.SharedKernel.Enums;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigiTekShop.Identity.Services;

public sealed class LoginService : ILoginService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly ISecurityEventService _securityEventService;
    private readonly SecuritySettings _securitySettings;
    private readonly ILogger<LoginService> _logger;

    public LoginService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtTokenService jwtTokenService,
        ILoginAttemptService loginAttemptService,
        ISecurityEventService securityEventService,
        IOptions<SecuritySettings> securitySettings,
        ILogger<LoginService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _loginAttemptService = loginAttemptService ?? throw new ArgumentNullException(nameof(loginAttemptService));
        _securityEventService = securityEventService ?? throw new ArgumentNullException(nameof(securityEventService));
        _securitySettings = securitySettings?.Value ?? throw new ArgumentNullException(nameof(securitySettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<TokenResponseDto>.Failure("Email and password are required.");

      
        if (_securitySettings.BruteForce.Enabled)
        {
            await CheckBruteForceAsync(request.Ip, request.DeviceId, ct);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
           
            await _loginAttemptService.RecordLoginAttemptAsync(
                userId: null,
                status: LoginStatus.Failed,
                ipAddress: request.Ip,
                userAgent: request.UserAgent,
                loginNameOrEmail: request.Email,
                ct: ct);
                
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_CREDENTIALS), IdentityErrorCodes.INVALID_CREDENTIALS);
        }

        
         if (user.IsDeleted) return Result<TokenResponseDto>.Failure("User not found or inactive.");

        if (await _userManager.IsLockedOutAsync(user))
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.ACCOUNT_LOCKED), IdentityErrorCodes.ACCOUNT_LOCKED);

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signIn.Succeeded)
        {
            
            await _loginAttemptService.RecordLoginAttemptAsync(
                userId: user.Id,
                status: LoginStatus.Success,
                ipAddress: request.Ip,
                userAgent: request.UserAgent,
                loginNameOrEmail: request.Email,
                ct: ct);
            
            if (await _userManager.GetTwoFactorEnabledAsync(user))
                return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.REQUIRES_TWO_FACTOR), IdentityErrorCodes.REQUIRES_TWO_FACTOR);

            
            if (_securitySettings.StepUp.Enabled && _securitySettings.StepUp.RequiredForNewDevices && !string.IsNullOrWhiteSpace(request.DeviceId))
            {
                var requiresStepUp = await CheckStepUpMfaRequiredAsync(user.Id, request.DeviceId, ct);
                if (requiresStepUp)
                {
                    _logger.LogInformation("Step-Up MFA required for user {UserId} on device {DeviceId}", user.Id, request.DeviceId);
                    return Result<TokenResponseDto>.Failure("Step-Up MFA required for new device", "STEP_UP_MFA_REQUIRED");
                }
            }

           
            var tokens = await _jwtTokenService.GenerateTokensAsync(
                userId: user.Id.ToString(),
                deviceId: request.DeviceId,
                ipAddress: request.Ip,
                userAgent: request.UserAgent,
                ct: ct);

            return tokens;
        }

        
        await _loginAttemptService.RecordLoginAttemptAsync(
            userId: user.Id,
            status: LoginStatus.Failed,
            ipAddress: request.Ip,
            userAgent: request.UserAgent,
            loginNameOrEmail: request.Email,
            ct: ct);

        
        if (_securitySettings.BruteForce.Enabled)
        {
            await CheckBruteForceAfterFailureAsync(request.Ip, request.DeviceId, user.Id, ct);
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

    #region Brute Force Detection

    private async Task CheckBruteForceAsync(string? ipAddress, string? deviceId, CancellationToken ct = default)
    {
        if (!_securitySettings.BruteForce.Enabled)
            return;

        try
        {
            // بررسی IP-based Brute Force
            if (_securitySettings.BruteForce.IpBasedLockout && !string.IsNullOrWhiteSpace(ipAddress))
            {
                var ipFailedAttempts = await _loginAttemptService.GetFailedAttemptsFromIpAsync(
                    ipAddress, _securitySettings.BruteForce.TimeWindow, ct);

                if (ipFailedAttempts.IsSuccess && ipFailedAttempts.Value >= _securitySettings.BruteForce.MaxFailedAttemptsPerIp)
                {
                    _logger.LogWarning("IP-based brute force detected: IP {IpAddress} has {Count} failed attempts", 
                        ipAddress, ipFailedAttempts.Value);

                    await _securityEventService.RecordSecurityEventAsync(
                        SecurityEventType.BruteForceAttempt,
                        metadata: new 
                        { 
                            Type = "IP-based",
                            IpAddress = ipAddress,
                            FailedAttempts = ipFailedAttempts.Value,
                            TimeWindow = _securitySettings.BruteForce.TimeWindow.ToString()
                        },
                        ipAddress: ipAddress,
                        ct: ct);

                    throw new InvalidOperationException("Too many failed login attempts from this IP address");
                }
            }

            // بررسی Device-based Brute Force
            if (_securitySettings.BruteForce.DeviceBasedLockout && !string.IsNullOrWhiteSpace(deviceId))
            {
                // اینجا می‌توانید منطق Device-based Brute Force را پیاده‌سازی کنید
                // برای سادگی، فعلاً فقط IP-based را پیاده‌سازی می‌کنیم
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking brute force for IP {IpAddress}", ipAddress);
            // در صورت خطا، اجازه ورود را می‌دهیم تا سیستم مسدود نشود
        }
    }

   
    private async Task CheckBruteForceAfterFailureAsync(string? ipAddress, string? deviceId, Guid userId, CancellationToken ct = default)
    {
        if (!_securitySettings.BruteForce.Enabled)
            return;

        try
        {
            // بررسی IP-based Brute Force
            if (_securitySettings.BruteForce.IpBasedLockout && !string.IsNullOrWhiteSpace(ipAddress))
            {
                var ipFailedAttempts = await _loginAttemptService.GetFailedAttemptsFromIpAsync(
                    ipAddress, _securitySettings.BruteForce.TimeWindow, ct);

                if (ipFailedAttempts.IsSuccess && ipFailedAttempts.Value >= _securitySettings.BruteForce.MaxFailedAttemptsPerIp)
                {
                    _logger.LogWarning("IP-based brute force detected after failure: IP {IpAddress} has {Count} failed attempts", 
                        ipAddress, ipFailedAttempts.Value);

                    await _securityEventService.RecordSecurityEventAsync(
                        SecurityEventType.BruteForceAttempt,
                        metadata: new 
                        { 
                            Type = "IP-based",
                            IpAddress = ipAddress,
                            FailedAttempts = ipFailedAttempts.Value,
                            TimeWindow = _securitySettings.BruteForce.TimeWindow.ToString(),
                            UserId = userId
                        },
                        userId: userId,
                        ipAddress: ipAddress,
                        ct: ct);
                }
            }

            // بررسی User-based Brute Force
            var userFailedAttempts = await _loginAttemptService.GetUserLoginAttemptsAsync(userId, 10, ct);
            if (userFailedAttempts.IsSuccess)
            {
                var recentFailedAttempts = userFailedAttempts.Value
                    .Where(la => la.Status == LoginStatus.Failed && 
                                la.AttemptedAt >= DateTime.UtcNow - _securitySettings.BruteForce.TimeWindow)
                    .Count();

                if (recentFailedAttempts >= _securitySettings.BruteForce.MaxFailedAttempts)
                {
                    _logger.LogWarning("User-based brute force detected: User {UserId} has {Count} failed attempts", 
                        userId, recentFailedAttempts);

                    await _securityEventService.RecordSecurityEventAsync(
                        SecurityEventType.BruteForceAttempt,
                        metadata: new 
                        { 
                            Type = "User-based",
                            UserId = userId,
                            FailedAttempts = recentFailedAttempts,
                            TimeWindow = _securitySettings.BruteForce.TimeWindow.ToString()
                        },
                        userId: userId,
                        ct: ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking brute force after failure for user {UserId}", userId);
        }
    }

    #endregion

    #region Step-Up MFA

   
    private async Task<bool> CheckStepUpMfaRequiredAsync(Guid userId, string deviceId, CancellationToken ct = default)
    {
        try
        {
            // بررسی اینکه آیا دستگاه جدید است یا خیر
            var userDevices = await _loginAttemptService.GetUserLoginAttemptsAsync(userId, 1, ct);
            if (userDevices.IsSuccess && !userDevices.Value.Any())
            {
                // کاربر هیچ تلاش ورودی نداشته، احتمالاً دستگاه جدید است
                return true;
            }

            // بررسی اینکه آیا دستگاه قبلاً استفاده شده یا خیر
            // اینجا می‌توانید منطق پیچیده‌تری برای تشخیص دستگاه جدید پیاده‌سازی کنید
            // برای سادگی، فعلاً فقط بررسی می‌کنیم که آیا دستگاه در لیست دستگاه‌های قابل اعتماد است یا خیر
            
            return false; // فعلاً Step-Up را غیرفعال می‌کنیم
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Step-Up MFA requirement for user {UserId} device {DeviceId}", userId, deviceId);
            return false; // در صورت خطا، Step-Up را غیرفعال می‌کنیم
        }
    }

    #endregion

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
        var errors = new List<string>();

        // Revoke refresh token
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var refreshResult = await _jwtTokenService.RevokeRefreshTokenAsync(request.RefreshToken, reason: "User logout", ct);
            if (refreshResult.IsFailure)
                errors.Add($"Refresh token revocation failed: {refreshResult.GetFirstError()}");
        }

        // Revoke access token (add to blacklist for immediate invalidation)
        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            var accessResult = await _jwtTokenService.RevokeAccessTokenAsync(request.AccessToken, reason: "User logout", ct);
            if (accessResult.IsFailure)
                errors.Add($"Access token revocation failed: {accessResult.GetFirstError()}");
        }

        // Even if some failures, logout should succeed (tokens might already be invalid)
        if (errors.Any())
            _logger.LogWarning("Logout completed with warnings: {Warnings}", string.Join("; ", errors));

        return Result.Success();
    }

    public async Task<Result> LogoutAllDevicesAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure("User id is required.");

        if (!Guid.TryParse(userId, out var userGuid))
            return Result.Failure("Invalid user id format.");

        // Revoke all refresh tokens
        var refreshResult = await _jwtTokenService.RevokeAllUserTokensAsync(userId, reason: "User logout all devices", ct);
        
        // Revoke all access tokens (user-level revocation)
        var accessResult = await _jwtTokenService.RevokeAllUserAccessTokensAsync(userGuid, reason: "User logout all devices", ct);

        if (refreshResult.IsFailure || accessResult.IsFailure)
        {
            var errors = new List<string>();
            if (refreshResult.IsFailure) errors.Add($"Refresh: {refreshResult.GetFirstError()}");
            if (accessResult.IsFailure) errors.Add($"Access: {accessResult.GetFirstError()}");
            
            _logger.LogError("Failed to logout all devices: {Errors}", string.Join("; ", errors));
            return Result.Failure(string.Join("; ", errors));
        }

        return Result.Success();
    }
}
