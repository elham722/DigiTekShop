using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.Abstractions.Identity.Security;
using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Identity.Options.Security;
using DigiTekShop.SharedKernel.Enums.Auth;
using DigiTekShop.SharedKernel.Enums.Security;

namespace DigiTekShop.Identity.Services.Login;

public sealed class LoginService : ILoginService
{
    private readonly ICurrentClient _client;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly ISecurityEventService _securityEventService;
    private readonly SecuritySettings _securitySettings;
    private readonly ILogger<LoginService> _logger;

    public LoginService(
        ICurrentClient client,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtTokenService jwtTokenService,
        ILoginAttemptService loginAttemptService,
        ISecurityEventService securityEventService,
        IOptions<SecuritySettings> securitySettings,
        ILogger<LoginService> logger)
    {
        _client = client;
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
        var deviceId = _client.DeviceId;
        var userAgent = _client.UserAgent;
        var ip = _client.IpAddress;

        var blockReason = await GetBruteForceBlockReasonAsync(ip, deviceId, ct);
        if (blockReason is not null)
            return Result<TokenResponseDto>.Failure("Too many failed attempts. Try later.", blockReason);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            await _loginAttemptService.RecordLoginAttemptAsync(null, LoginStatus.Failed, ip, userAgent, request.Email, ct);
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.INVALID_CREDENTIALS);

        }

        if (user.IsDeleted)
            return Result<TokenResponseDto>.Failure("User not found or inactive.");

        if (await _userManager.IsLockedOutAsync(user))
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.ACCOUNT_LOCKED);


        var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signIn.Succeeded)
        {
            await _loginAttemptService.RecordLoginAttemptAsync(user.Id, LoginStatus.Success, ip, userAgent, request.Email, ct);

            if (await _userManager.GetTwoFactorEnabledAsync(user))
                return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.REQUIRES_TWO_FACTOR);


            // Step-Up بر اساس دستگاه
            if (_securitySettings.StepUp.Enabled && _securitySettings.StepUp.RequiredForNewDevices && !string.IsNullOrWhiteSpace(deviceId))
            {
                var requiresStepUp = await CheckStepUpMfaRequiredAsync(user.Id,deviceId , ct);
                if (requiresStepUp)
                    return Result<TokenResponseDto>.Failure("Step-Up MFA required for new device", "STEP_UP_MFA_REQUIRED");
            }

            // موفقیت کامل → ثبت آخرین ورود
            user.RecordLogin(DateTime.UtcNow);
            await _userManager.UpdateAsync(user);

            // تولید توکن‌ها (ManageUserDevice در JwtTokenService شما انجام می‌شود)
            var tokens = await _jwtTokenService.GenerateTokensAsync(user.Id.ToString(),ip,deviceId,userAgent, ct);
            return tokens;
        }

        // شکست
        await _loginAttemptService.RecordLoginAttemptAsync(user.Id, LoginStatus.Failed, ip, userAgent, request.Email, ct);

        // چک Brute-force پس از شکست
        await CheckBruteForceAfterFailureAsync(ip, deviceId, user.Id, ct);

        if (signIn.IsLockedOut)
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.ACCOUNT_LOCKED);

        if (signIn.RequiresTwoFactor)
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.REQUIRES_TWO_FACTOR);
        if (signIn.IsNotAllowed)
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.SIGNIN_NOT_ALLOWED);

        return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.INVALID_CREDENTIALS);

    }

    #region Brute Force Detection

    private async Task<string?> GetBruteForceBlockReasonAsync(string? ipAddress, string? deviceId, CancellationToken ct)
    {
        if (!_securitySettings.BruteForce.Enabled) return null;

        try
        {
            if (_securitySettings.BruteForce.IpBasedLockout && !string.IsNullOrWhiteSpace(ipAddress))
            {
                var r = await _loginAttemptService.GetFailedAttemptsFromIpAsync(ipAddress, _securitySettings.BruteForce.TimeWindow, ct);
                if (r.IsSuccess && r.Value >= _securitySettings.BruteForce.MaxFailedAttemptsPerIp)
                {
                    await _securityEventService.RecordSecurityEventAsync(
                        SecurityEventType.BruteForceAttempt,
                        metadata: new { Type = "IP-based", IpAddress = ipAddress, Failed = r.Value, Window = _securitySettings.BruteForce.TimeWindow.ToString() },
                        ipAddress: ipAddress,
                        ct: ct);
                    return "TOO_MANY_ATTEMPTS_IP";
                }
            }

            // اگر DeviceBased هم خواستی، همین‌جا اضافه کن…
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking brute force for IP {Ip}", ipAddress);
            return null; // fail-open
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


    private async Task<bool> CheckStepUpMfaRequiredAsync(Guid userId, string deviceId, CancellationToken ct)
    {
        try
        {

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return true;


            return !user.HasDeviceWithFingerprint(deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Step-Up for user {UserId} device {DeviceId}", userId, deviceId);
            return false; // fail-open
        }
    }
    #endregion

    public async Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.INVALID_TOKEN);

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
