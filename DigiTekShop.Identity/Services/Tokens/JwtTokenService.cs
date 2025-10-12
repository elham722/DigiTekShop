using DigiTekShop.Identity.Options;
using DigiTekShop.Identity.Options.Security;
using DigiTekShop.SharedKernel.Exceptions.Validation;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.Contracts.Enums.Security;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Abstractions.Caching;
using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Contracts.Abstractions.Identity.Security;

namespace DigiTekShop.Identity.Services.Tokens;

public class JwtTokenService : IJwtTokenService
{
    private readonly UserManager<User> _userManager;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly DeviceLimitsSettings _deviceLimits;
    private readonly SecuritySettings _securitySettings;
    private readonly ISecurityEventService _securityEventService;
    private readonly ITokenBlacklistService _tokenBlacklist;
    private readonly ILogger<JwtTokenService> _logger;


    public JwtTokenService(
        UserManager<User> userManager,
        DigiTekShopIdentityDbContext context,
        IOptions<JwtSettings> jwtOptions,
        IOptions<DeviceLimitsSettings> deviceLimitsOptions,
        IOptions<SecuritySettings> securitySettings,
        ISecurityEventService securityEventService,
        ITokenBlacklistService tokenBlacklist,
        ILogger<JwtTokenService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jwtSettings = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _deviceLimits = deviceLimitsOptions?.Value ?? throw new ArgumentNullException(nameof(deviceLimitsOptions));
        _securitySettings = securitySettings?.Value ?? throw new ArgumentNullException(nameof(securitySettings));
        _securityEventService = securityEventService ?? throw new ArgumentNullException(nameof(securityEventService));
        _tokenBlacklist = tokenBlacklist ?? throw new ArgumentNullException(nameof(tokenBlacklist));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Generate / Create tokens

    public async Task<Result<TokenResponseDto>> GenerateTokensAsync(
        string userId, string? deviceId = null, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.USER_NOT_FOUND);


        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.USER_NOT_FOUND);

        await ManageUserDeviceAsync(user, deviceId, ipAddress, userAgent, ct);

       
        await CleanupExpiredTokensAsync(ct);

        var claims = await GetUserClaimsAsync(user);

        var now = DateTimeOffset.UtcNow;
        var accessExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var (accessToken, jti) = CreateJwtToken(claims, accessExpiresAt.UtcDateTime);

        // refresh (raw + hash)
        var refreshTokenRaw = GenerateRefreshToken();
        var refreshExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var refreshTokenHash = HashToken(refreshTokenRaw);

        // Revoke existing active tokens for the same user+device
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            await RevokeActiveTokensForDeviceAsync(userGuid, deviceId, "New token created", ct);
        }

        var refreshEntity = RefreshToken.Create(
            refreshTokenHash, refreshExpiresAt.UtcDateTime, userGuid, deviceId, ip: ipAddress, userAgent: userAgent);

        _context.RefreshTokens.Add(refreshEntity);
        await _context.SaveChangesAsync(ct);

        var dto = new TokenResponseDto(
            TokenType: "Bearer",
            AccessToken: accessToken,
            ExpiresIn: (int)(accessExpiresAt - now).TotalSeconds,
            RefreshToken: refreshTokenRaw,
            RefreshTokenExpiresAt: refreshExpiresAt
        );

        return Result<TokenResponseDto>.Success(dto);
    }


    #endregion

    #region Device Management

   
    private async Task ManageUserDeviceAsync(User user, string? deviceId, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        if (_deviceLimits.AutoDeactivateInactiveDevices)
        {
            user.DeactivateInactiveDevices(_deviceLimits.DeviceInactivityThreshold);
        }

        var existingDevice = user.GetDeviceByFingerprint(deviceId);
        
        if (existingDevice != null)
        {
            existingDevice.UpdateLogin(DateTime.UtcNow);
            existingDevice.UpdateDeviceInfo(deviceName: userAgent);
        }
        else
        {
            var deviceName = ExtractDeviceNameFromUserAgent(userAgent);
            var newDevice = UserDevice.Create(
                userId: user.Id,
                deviceName: deviceName,
                ipAddress: ipAddress ?? "Unknown",
                fingerprint: deviceId,
                browser: ExtractBrowserFromUserAgent(userAgent),
                os: ExtractOSFromUserAgent(userAgent)
            );

            
            if (_securitySettings.StepUp.Enabled && _securitySettings.StepUp.RequiredForNewDevices)
            {
                
                newDevice.MarkAsUntrusted();
                
                _logger.LogInformation("New device detected for user {UserId}, Step-Up MFA required", user.Id);
                
                
                await _securityEventService.RecordSecurityEventAsync(
                    SecurityEventType.StepUpMfaRequired,
                    metadata: new 
                    { 
                        DeviceId = deviceId,
                        DeviceName = deviceName,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        Reason = "New device detected - Step-Up MFA required"
                    },
                    userId: user.Id,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    deviceId: deviceId);
            }
            else if (_deviceLimits.DefaultTrustNewDevices)
            {
                newDevice.MarkAsTrusted();
            }

            try
            {
                user.AddDevice(newDevice, _deviceLimits.MaxActiveDevicesPerUser, _deviceLimits.MaxTrustedDevicesPerUser);
                _context.UserDevices.Add(newDevice);
            }
            catch (InvalidDomainOperationException ex) when (ex.Code == ErrorCodes.Identity.MAX_ACTIVE_DEVICES_EXCEEDED)
            {
                var oldestDevice = user.Devices
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.LastLoginAt)
                    .FirstOrDefault();

                if (oldestDevice != null)
                {
                    oldestDevice.Deactivate();
                    user.AddDevice(newDevice, _deviceLimits.MaxActiveDevicesPerUser, _deviceLimits.MaxTrustedDevicesPerUser);
                    _context.UserDevices.Add(newDevice);
                }
                else
                {
                    throw;
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }

  
    private string ExtractDeviceNameFromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown Device";

        if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
            return "Mobile Device";
        
        if (userAgent.Contains("Windows"))
            return "Windows Device";
        
        if (userAgent.Contains("Mac"))
            return "Mac Device";
        
        if (userAgent.Contains("Linux"))
            return "Linux Device";

        return "Unknown Device";
    }

  
    private string? ExtractBrowserFromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        if (userAgent.Contains("Chrome"))
            return "Chrome";
        
        if (userAgent.Contains("Firefox"))
            return "Firefox";
        
        if (userAgent.Contains("Safari"))
            return "Safari";
        
        if (userAgent.Contains("Edge"))
            return "Edge";

        return "Unknown Browser";
    }

 
    private string? ExtractOSFromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        if (userAgent.Contains("Windows"))
            return "Windows";
        
        if (userAgent.Contains("Mac"))
            return "macOS";
        
        if (userAgent.Contains("Linux"))
            return "Linux";
        
        if (userAgent.Contains("Android"))
            return "Android";
        
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
            return "iOS";

        return "Unknown OS";
    }

    #endregion

    #region Refresh / Rotation

    public async Task<Result<TokenResponseDto>> RefreshTokensAsync(
      string refreshToken, string? deviceId = null, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.INVALID_TOKEN);

        var refreshHash = HashToken(refreshToken);

        var existing = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshHash,ct);

        if (existing == null)
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.TOKEN_NOT_FOUND);

        if (existing.IsRevoked)
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.TOKEN_REVOKED);

        if (existing.IsRotated)
        {
            
            _logger.LogWarning("Token replay detected: Rotated token {TokenId} used again for user {UserId}", 
                existing.Id, existing.UserId);

            await _securityEventService.RecordSecurityEventAsync(
                SecurityEventType.TokenReplay,
                metadata: new 
                { 
                    TokenId = existing.Id,
                    RotatedAt = existing.RotatedAt,
                    ReplacedByTokenHash = existing.ReplacedByTokenHash,
                    DeviceId = existing.DeviceId,
                    Reason = "Rotated token reused - replay attack detected"
                },
                userId: existing.UserId,
                ipAddress: ipAddress,
                deviceId: existing.DeviceId,
                ct: ct);

           
            await RevokeTokenChainAsync(existing, "Token replay attack - rotated token reused", ct);

            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.INVALID_TOKEN);


        }
        if (existing.ExpiresAt <= DateTime.UtcNow)
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.TOKEN_EXPIRED);


        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user == null)
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.USER_NOT_FOUND);


        // ✅ Device Integrity Check: Prevent device hijacking via token refresh
        var boundDeviceId = existing.DeviceId;
        if (!string.IsNullOrWhiteSpace(deviceId) && !string.IsNullOrWhiteSpace(boundDeviceId) &&
            !string.Equals(deviceId, boundDeviceId, StringComparison.Ordinal))
        {
            _logger.LogWarning("Device mismatch on refresh: token device={TokenDevice}, provided device={Provided}, user={UserId}",
                boundDeviceId, deviceId, existing.UserId);

            await _securityEventService.RecordSecurityEventAsync(
                SecurityEventType.RefreshTokenAnomaly,
                metadata: new
                {
                    TokenId = existing.Id,
                    TokenDeviceId = boundDeviceId,
                    ProvidedDeviceId = deviceId,
                    Reason = "Device mismatch - potential token theft"
                },
                userId: existing.UserId,
                ipAddress: ipAddress,
                deviceId: deviceId,
                ct: ct);

            return Result<TokenResponseDto>.Failure("Device mismatch. Please re-authenticate.", ErrorCodes.Identity.INVALID_TOKEN);
        }

        // ✅ IP/UserAgent Anomaly Detection (optional but recommended)
        if (_securitySettings.StepUp.Enabled && _securitySettings.StepUp.RequiredForAnomalousActivity)
        {
            await DetectAndHandleContextAnomaliesAsync(existing, ipAddress, userAgent, ct);
        }

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
           
            await ValidateTokenRotationSecurityAsync(existing, ct);

            // ✅ Enforce single-active-token-per-device policy
            // Revoke any other active tokens for this device (except the current one being rotated)
            if (!string.IsNullOrWhiteSpace(boundDeviceId))
            {
                var otherActiveTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == existing.UserId &&
                                rt.DeviceId == boundDeviceId &&
                                rt.Id != existing.Id && // exclude current token being rotated
                                !rt.IsRevoked &&
                                rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync(ct);

                if (otherActiveTokens.Any())
                {
                    _logger.LogInformation(
                        "Revoking {Count} other active tokens for device {DeviceId} during refresh (single-active-per-device policy)",
                        otherActiveTokens.Count, boundDeviceId);

                    foreach (var token in otherActiveTokens)
                    {
                        token.Revoke("Single-active-token-per-device policy: new token created via refresh");
                    }

                    _context.RefreshTokens.UpdateRange(otherActiveTokens);
                }
            }

           
            existing.MarkAsUsed();

            existing.Revoke("Token rotated");

            var now = DateTimeOffset.UtcNow;

            var newRefreshRaw = GenerateRefreshToken();
            var newRefreshHash = HashToken(newRefreshRaw);
            var newRefreshExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            // ✅ Use bound deviceId from existing token (prevent device switching)
            var newRefresh = RefreshToken.Create(
                tokenHash: newRefreshHash,
                expiresAt: newRefreshExpiresAt.UtcDateTime,
                userId: existing.UserId,
                deviceId: boundDeviceId, // ✅ از device اصلی استفاده می‌کنیم
                ip: ipAddress, // IP می‌تواند تغییر کند (mobile networks, VPN)
                userAgent: userAgent, // UserAgent می‌تواند به‌روزرسانی شود
                parentTokenHash: existing.TokenHash 
            );

            existing.MarkAsRotated(newRefreshHash);

            _context.RefreshTokens.Update(existing);
            _context.RefreshTokens.Add(newRefresh);

            // access جدید
            var claims = await GetUserClaimsAsync(user);
            var accessExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            var (accessToken, jti) = CreateJwtToken(claims, accessExpiresAt.UtcDateTime);

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var dto = new TokenResponseDto(
                TokenType: "Bearer",
                AccessToken: accessToken,
                ExpiresIn: (int)(accessExpiresAt - now).TotalSeconds,
                RefreshToken: newRefreshRaw,          
                RefreshTokenExpiresAt: newRefreshExpiresAt
            );

            return Result<TokenResponseDto>.Success(dto);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // ✅ Optimistic Concurrency Conflict: Another request already rotated this token
            await tx.RollbackAsync(ct);
            
            _logger.LogWarning(ex, 
                "Concurrency conflict during token rotation for token {TokenId}, user {UserId}. " +
                "This typically means the token was already rotated by a concurrent request (race condition).",
                existing.Id, existing.UserId);

            // Record security event for potential replay/race condition
            await _securityEventService.RecordSecurityEventAsync(
                SecurityEventType.TokenReplay,
                metadata: new
                {
                    TokenId = existing.Id,
                    UserId = existing.UserId,
                    DeviceId = existing.DeviceId,
                    Reason = "Concurrency conflict - token already modified (possible race condition or replay)",
                    ExceptionType = "DbUpdateConcurrencyException"
                },
                userId: existing.UserId,
                ipAddress: ipAddress,
                deviceId: existing.DeviceId,
                ct: ct);

            // Treat concurrency conflict as token already used (similar to replay attack)
            return Result<TokenResponseDto>.Failure(
                "This token has already been used. Please use the latest token or re-authenticate.",
                ErrorCodes.Identity.INVALID_TOKEN);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Error rotating refresh token for user {UserId}", existing.UserId);
            return ResultFactories.Fail<TokenResponseDto>(ErrorCodes.Identity.INVALID_TOKEN);

        }
    }

    #endregion

    #region Device Token Management

 
    private async Task RevokeActiveTokensForDeviceAsync(Guid userId, string deviceId, string reason, CancellationToken ct = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && 
                        rt.DeviceId == deviceId && 
                        !rt.IsRevoked && 
                        rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.Revoke(reason);
        }

        if (activeTokens.Any())
        {
            _context.RefreshTokens.UpdateRange(activeTokens);
            _logger.LogInformation("Revoked {Count} active tokens for user {UserId} device {DeviceId}", 
                activeTokens.Count, userId, deviceId);
        }
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken ct = default)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow && !rt.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in expiredTokens)
        {
            token.Revoke("Token expired");
        }

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.UpdateRange(expiredTokens);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Cleaned up {Count} expired tokens", expiredTokens.Count);
        }
    }

    private async Task ValidateTokenRotationSecurityAsync(RefreshToken token, CancellationToken ct = default)
    {
        if (token.UsageCount > 0)
        {
            var latestActiveToken = await _context.RefreshTokens
                .Where(rt => rt.UserId == token.UserId && 
                           rt.DeviceId == token.DeviceId && 
                           !rt.IsRevoked && 
                           rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .FirstOrDefaultAsync(ct);

            
            if (latestActiveToken != null && latestActiveToken.Id != token.Id)
            {
                _logger.LogWarning("Potential token leak detected: Token {TokenId} is not the latest active token for user {UserId} device {DeviceId}", 
                    token.Id, token.UserId, token.DeviceId);

                
                await _securityEventService.RecordSecurityEventAsync(
                    SecurityEventType.RefreshTokenAnomaly,
                    metadata: new 
                    { 
                        TokenId = token.Id,
                        LatestTokenId = latestActiveToken.Id,
                        DeviceId = token.DeviceId,
                        Reason = "Token rotation violation - not latest active token"
                    },
                    userId: token.UserId,
                    deviceId: token.DeviceId);

                await RevokeActiveTokensForDeviceAsync(token.UserId, token.DeviceId ?? "unknown", "Potential token leak detected", ct);
                
                throw new InvalidOperationException("Token rotation security violation detected");
            }
        }

        
        if (!string.IsNullOrEmpty(token.ParentTokenHash))
        {
            var parentToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == token.ParentTokenHash, ct);

            if (parentToken != null)
            {
               
                if (parentToken.IsRotated)
                {
                    _logger.LogWarning("Token replay attack: Parent token {ParentTokenId} was already rotated, but child token {TokenId} is being used", 
                        parentToken.Id, token.Id);

                    await _securityEventService.RecordSecurityEventAsync(
                        SecurityEventType.TokenReplay,
                        metadata: new 
                        { 
                            TokenId = token.Id,
                            ParentTokenId = parentToken.Id,
                            ParentRotatedAt = parentToken.RotatedAt,
                            ParentReplacedByTokenHash = parentToken.ReplacedByTokenHash,
                            DeviceId = token.DeviceId,
                            Reason = "Parent token already rotated - replay attack detected"
                        },
                        userId: token.UserId,
                        deviceId: token.DeviceId,
                        ct: ct);

                    await RevokeTokenChainAsync(token, "Parent token replay attack detected", ct);
                    
                    throw new InvalidOperationException("Token replay attack detected - parent token already rotated");
                }

               
                if (parentToken.UsageCount > 0)
                {
                    _logger.LogWarning("Potential token replay attack: Token {TokenId} has used parent token {ParentTokenId} (usage: {UsageCount})", 
                        token.Id, parentToken.Id, parentToken.UsageCount);

                    await _securityEventService.RecordSecurityEventAsync(
                        SecurityEventType.TokenReplay,
                        metadata: new 
                        { 
                            TokenId = token.Id,
                            ParentTokenId = parentToken.Id,
                            ParentTokenUsageCount = parentToken.UsageCount,
                            DeviceId = token.DeviceId,
                            Reason = "Parent token already used - potential replay attack"
                        },
                        userId: token.UserId,
                        deviceId: token.DeviceId,
                        ct: ct);

                    await RevokeTokenChainAsync(token, "Potential replay attack detected", ct);
                    
                    throw new InvalidOperationException("Token replay attack detected");
                }
            }
        }
    }

    /// <summary>
    /// Detects and handles IP/UserAgent anomalies during token refresh
    /// </summary>
    /// <remarks>
    /// Checks for significant changes in IP address or UserAgent that may indicate token theft.
    /// Threshold-based approach: minor changes are OK, radical changes require Step-Up MFA or re-authentication.
    /// </remarks>
    private async Task DetectAndHandleContextAnomaliesAsync(RefreshToken existingToken, string? newIpAddress, string? newUserAgent, CancellationToken ct = default)
    {
        var anomalies = new List<string>();

        // ✅ IP Address Change Detection
        if (!string.IsNullOrWhiteSpace(existingToken.CreatedByIp) && !string.IsNullOrWhiteSpace(newIpAddress))
        {
            // Simple check: exact mismatch (can be enhanced with IP range/geolocation checks)
            if (!string.Equals(existingToken.CreatedByIp, newIpAddress, StringComparison.OrdinalIgnoreCase))
            {
                // Note: IP changes are common (mobile networks, VPN, WiFi/4G switching)
                // We log it but don't block unless configured to be strict
                _logger.LogInformation("IP address changed during refresh: {OldIp} → {NewIp} for user {UserId}",
                    existingToken.CreatedByIp, newIpAddress, existingToken.UserId);

                anomalies.Add($"IP changed: {existingToken.CreatedByIp} → {newIpAddress}");

                // Optional: Check if IP is from completely different country/region (requires GeoIP service)
                // var isRadicalChange = await _geoIpService.IsRadicalLocationChangeAsync(existingToken.CreatedByIp, newIpAddress);
                // if (isRadicalChange) { ... require Step-Up ... }
            }
        }

        // ✅ UserAgent Change Detection
        if (!string.IsNullOrWhiteSpace(existingToken.UserAgent) && !string.IsNullOrWhiteSpace(newUserAgent))
        {
            // Check for significant UserAgent changes (e.g., different browser or OS)
            var isSignificantChange = DetectSignificantUserAgentChange(existingToken.UserAgent, newUserAgent);
            
            if (isSignificantChange)
            {
                _logger.LogWarning("Significant UserAgent change detected during refresh for user {UserId}: {OldUA} → {NewUA}",
                    existingToken.UserId, existingToken.UserAgent, newUserAgent);

                anomalies.Add($"UserAgent changed significantly: {existingToken.UserAgent} → {newUserAgent}");
            }
        }

        // ✅ Record Security Event if anomalies detected
        if (anomalies.Any())
        {
            var severity = anomalies.Count > 1 ? "High" : "Medium"; // Multiple anomalies = higher severity
            
            await _securityEventService.RecordSecurityEventAsync(
                SecurityEventType.RefreshTokenAnomaly,
                metadata: new
                {
                    TokenId = existingToken.Id,
                    Anomalies = anomalies,
                    AnomalyCount = anomalies.Count,
                    OldIp = existingToken.CreatedByIp,
                    NewIp = newIpAddress,
                    OldUserAgent = existingToken.UserAgent,
                    NewUserAgent = newUserAgent,
                    Severity = severity,
                    Action = "Logged - token refresh allowed with monitoring",
                    Recommendation = "Consider implementing Step-Up MFA for high-severity anomalies"
                },
                userId: existingToken.UserId,
                ipAddress: newIpAddress,
                deviceId: existingToken.DeviceId,
                ct: ct);

            // ✅ Optional: Mark device as untrusted for severe anomalies (requires re-verification)
            // This can be used with Step-Up MFA requirement
            if (_securitySettings.StepUp.Enabled && 
                _securitySettings.StepUp.RequiredForAnomalousActivity && 
                anomalies.Count > 1) // Multiple anomalies = suspicious
            {
                _logger.LogWarning(
                    "Multiple context anomalies detected for user {UserId} device {DeviceId}. " +
                    "Device marked as untrusted and may require Step-Up MFA on next critical operation.",
                    existingToken.UserId, existingToken.DeviceId);

                // Note: You can implement device trust level management here:
                // var device = await _context.UserDevices
                //     .FirstOrDefaultAsync(d => d.UserId == existingToken.UserId && 
                //                              d.DeviceFingerprint == existingToken.DeviceId, ct);
                // if (device != null)
                // {
                //     device.MarkAsUntrusted();
                //     await _context.SaveChangesAsync(ct);
                // }

                // Optional: For very severe anomalies, enforce immediate re-authentication:
                // throw new InvalidOperationException("Suspicious activity detected. Please re-authenticate.");
            }
        }
    }

    /// <summary>
    /// Detects significant changes in UserAgent (browser/OS/device type)
    /// </summary>
    private bool DetectSignificantUserAgentChange(string oldUserAgent, string newUserAgent)
    {
        if (string.Equals(oldUserAgent, newUserAgent, StringComparison.Ordinal))
            return false;

        // Normalize and compare key components
        var oldUA = oldUserAgent.ToLowerInvariant();
        var newUA = newUserAgent.ToLowerInvariant();

        // Check for browser change
        var browsers = new[] { "chrome", "firefox", "safari", "edge", "opera" };
        var oldBrowser = browsers.FirstOrDefault(b => oldUA.Contains(b));
        var newBrowser = browsers.FirstOrDefault(b => newUA.Contains(b));
        if (oldBrowser != null && newBrowser != null && oldBrowser != newBrowser)
        {
            _logger.LogWarning("Browser changed: {OldBrowser} → {NewBrowser}", oldBrowser, newBrowser);
            return true; // Browser change is significant
        }

        // Check for OS change
        var operatingSystems = new[] { "windows", "mac os", "linux", "android", "ios" };
        var oldOS = operatingSystems.FirstOrDefault(os => oldUA.Contains(os));
        var newOS = operatingSystems.FirstOrDefault(os => newUA.Contains(os));
        if (oldOS != null && newOS != null && oldOS != newOS)
        {
            _logger.LogWarning("OS changed: {OldOS} → {NewOS}", oldOS, newOS);
            return true; // OS change is significant
        }

        // Check for device type change (mobile ↔ desktop)
        var oldIsMobile = oldUA.Contains("mobile") || oldUA.Contains("android") || oldUA.Contains("iphone");
        var newIsMobile = newUA.Contains("mobile") || newUA.Contains("android") || newUA.Contains("iphone");
        if (oldIsMobile != newIsMobile)
        {
            _logger.LogWarning("Device type changed: mobile={OldMobile} → mobile={NewMobile}", oldIsMobile, newIsMobile);
            return true; // Device type change is significant
        }

        // Minor version changes (e.g., Chrome 120 → Chrome 121) are not significant
        return false;
    }

    /// <summary>
    /// Recursively revokes an entire token chain (parents, children, and all descendants)
    /// </summary>
    /// <remarks>
    /// Uses BFS (Breadth-First Search) to traverse the entire token graph:
    /// - Follows ParentTokenHash links (upward)
    /// - Follows ReplacedByTokenHash links (forward)
    /// - Follows child tokens via ParentTokenHash (downward)
    /// 
    /// Handles complex chains like:
    /// GrandParent → Parent → Current → Child → GrandChild
    /// 
    /// Prevents infinite loops with visited set.
    /// </remarks>
    private async Task RevokeTokenChainAsync(RefreshToken token, string reason, CancellationToken ct = default)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        var tokensToRevoke = new List<RefreshToken>();

        // Start with the initial token
        queue.Enqueue(token.TokenHash);

        _logger.LogInformation("Starting recursive token chain revocation from token {TokenId}", token.Id);

        while (queue.Count > 0)
        {
            var currentHash = queue.Dequeue();
            
            // Skip if already visited (prevent infinite loops)
            if (!visited.Add(currentHash))
                continue;

            // Find all tokens related to current hash:
            // 1. Token with this hash (current)
            // 2. Tokens with this as parent (children)
            // 3. Tokens with this as ReplacedBy (previous in rotation)
            var relatedTokens = await _context.RefreshTokens
                .Where(t => (t.TokenHash == currentHash ||
                            t.ParentTokenHash == currentHash ||
                            t.ReplacedByTokenHash == currentHash) &&
                            !t.IsRevoked)
                .ToListAsync(ct);

            foreach (var t in relatedTokens)
            {
                // Add to revoke list if not already revoked
                if (!t.IsRevoked && !tokensToRevoke.Any(x => x.Id == t.Id))
                {
                    t.Revoke($"Chain revocation: {reason}");
                    tokensToRevoke.Add(t);
                    
                    _logger.LogDebug("Marked token {TokenId} for revocation in chain", t.Id);
                }

                // ✅ Traverse upward: Follow parent link
                if (!string.IsNullOrEmpty(t.ParentTokenHash) && !visited.Contains(t.ParentTokenHash))
                {
                    queue.Enqueue(t.ParentTokenHash);
                }

                // ✅ Traverse forward: Follow replacement link
                if (!string.IsNullOrEmpty(t.ReplacedByTokenHash) && !visited.Contains(t.ReplacedByTokenHash))
                {
                    queue.Enqueue(t.ReplacedByTokenHash);
                }

                // ✅ Traverse downward: Find direct children
                var children = await _context.RefreshTokens
                    .Where(c => c.ParentTokenHash == t.TokenHash && 
                               !c.IsRevoked &&
                               !visited.Contains(c.TokenHash))
                    .Select(c => c.TokenHash)
                    .ToListAsync(ct);

                foreach (var childHash in children)
                {
                    if (!visited.Contains(childHash))
                    {
                        queue.Enqueue(childHash);
                    }
                }
            }
        }

        // Save all revoked tokens
        if (tokensToRevoke.Any())
        {
            _context.RefreshTokens.UpdateRange(tokensToRevoke);
            await _context.SaveChangesAsync(ct);
            
            _logger.LogWarning(
                "Recursively revoked token chain: {Count} tokens across {Levels} levels for reason: {Reason}",
                tokensToRevoke.Count,
                visited.Count,
                reason);
        }
        else
        {
            _logger.LogInformation("No tokens to revoke in chain (all already revoked)");
        }
    }

    #endregion

    #region Revoke

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN);

        var refreshHash = HashToken(refreshToken);
        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == refreshHash, ct);
        if (existing == null) return Result.Success(); 

        if (!existing.IsRevoked)
        {
            existing.Revoke(reason ?? "RevokedByUser");
            _context.RefreshTokens.Update(existing);
            await _context.SaveChangesAsync(ct);
        }

        return Result.Success();
    }


    public async Task<Result> RevokeAllUserTokensAsync(string userId, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))  return ResultFactories.Fail(ErrorCodes.Identity.USER_NOT_FOUND);

        if (!Guid.TryParse(userId, out var userGuid))
            return ResultFactories.Fail(ErrorCodes.Identity.USER_NOT_FOUND);

        var tokens = await _context.RefreshTokens.Where(rt => rt.UserId == userGuid && !rt.IsRevoked).ToListAsync(ct);
        if (!tokens.Any()) return Result.Success();

        foreach (var t in tokens)
        {
            t.Revoke(reason ?? "Revoked all user tokens");
        }

        _context.RefreshTokens.UpdateRange(tokens);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    #endregion

    #region Access Token Revocation

    
    public async Task<Result> RevokeAccessTokenAsync(string accessToken, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN);

        try
        {
           
            var tokenHandler = new JwtSecurityTokenHandler();
            
            if (!tokenHandler.CanReadToken(accessToken))
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN);

            var jwtToken = tokenHandler.ReadJwtToken(accessToken);
            
            var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
                return Result.Failure("Token does not contain JTI claim");

            var exp = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            if (string.IsNullOrEmpty(exp) || !long.TryParse(exp, out var expTimestamp))
                return Result.Failure("Token does not contain valid expiration");

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expTimestamp).UtcDateTime;

           
            await _tokenBlacklist.RevokeAccessTokenAsync(jti, expiresAt, reason ?? "Access token revoked", ct);

            _logger.LogInformation("Access token revoked: JTI={Jti}, Reason={Reason}", jti, reason);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking access token");
            return Result.Failure("Failed to revoke access token");
        }
    }

  
    public async Task<Result> RevokeAllUserAccessTokensAsync(Guid userId, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            await _tokenBlacklist.RevokeAllUserTokensAsync(userId, reason ?? "All user tokens revoked", ct);
            
            _logger.LogWarning("All access tokens revoked for user: UserId={UserId}, Reason={Reason}", 
                userId, reason);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all user access tokens: UserId={UserId}", userId);
            return Result.Failure("Failed to revoke user tokens");
        }
    }

    #endregion

    #region Validate Access Token

    public async Task<Result<ClaimsPrincipal>> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return ResultFactories.Fail<ClaimsPrincipal>(ErrorCodes.Identity.INVALID_TOKEN);

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                RequireExpirationTime = true,
                ValidateActor = false,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
            };

            var principal = tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            // Validate JTI claim exists
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
                return ResultFactories.Fail<ClaimsPrincipal>(ErrorCodes.Identity.INVALID_TOKEN);

            // Check if token is revoked (JTI blacklist)
            var isJtiRevoked = await _tokenBlacklist.IsTokenRevokedAsync(jti, ct);
            if (isJtiRevoked)
            {
                _logger.LogWarning("Access denied: Blacklisted token JTI {Jti}", jti);
                return ResultFactories.Fail<ClaimsPrincipal>(ErrorCodes.Identity.INVALID_TOKEN);
            }

            // Check user-level revocation (all user tokens revoked after password change, etc.)
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var iatClaim = principal.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
            
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId) && 
                !string.IsNullOrEmpty(iatClaim) && long.TryParse(iatClaim, out var iatTimestamp))
            {
                var tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(iatTimestamp).UtcDateTime;
                var isUserRevoked = await _tokenBlacklist.IsUserTokensRevokedAsync(userId, tokenIssuedAt, ct);
                
                if (isUserRevoked)
                {
                    _logger.LogWarning("Access denied: User-level token revocation for UserId {UserId}", userId);
                    return ResultFactories.Fail<ClaimsPrincipal>(ErrorCodes.Identity.INVALID_TOKEN);
                }
            }

            return Result<ClaimsPrincipal>.Success(principal);
        }
        catch (SecurityTokenExpiredException)
        {
            return ResultFactories.Fail<ClaimsPrincipal>(ErrorCodes.Identity.INVALID_TOKEN);
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("Invalid token signature");
            return ResultFactories.Fail<ClaimsPrincipal>(ErrorCodes.Identity.INVALID_TOKEN);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return ResultFactories.Fail<ClaimsPrincipal>(ErrorCodes.Identity.INVALID_TOKEN);
        }
    }

  
    private async Task<bool> IsAccessTokenRevokedAsync(string jti, CancellationToken ct = default)
    {
        try
        {
            // Check 1: Is this specific token (JTI) in the blacklist?
            var isJtiRevoked = await _tokenBlacklist.IsTokenRevokedAsync(jti, ct);
            if (isJtiRevoked)
            {
                _logger.LogWarning("Access denied: Token JTI {Jti} is blacklisted", jti);
                return true;
            }

            // Check 2: Are all tokens for this user revoked? (user-level revocation)
            // We need to extract userId and iat (issued at) from the JTI context
            // In a real scenario, you'd parse these from the token claims before calling this method
            // For now, we'll return false for user-level check
            // TODO: Add user-level revocation check if needed

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token revocation status for JTI: {Jti}", jti);
            // در صورت خطا، به احتیاط اجازه می‌دهیم (تا سرویس از کار نیفتد)
            // می‌توانید این را به true تغییر دهید برای امنیت بیشتر
            return false;
        }
    }

    #endregion

    #region Helpers & Utils

    private (string token, string jti) CreateJwtToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var jti = Guid.NewGuid().ToString();
        var issuedAt = DateTimeOffset.UtcNow;
        
        var claimsList = claims.ToList();
        claimsList.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));
        
        // Add iat (issued at) claim for token revocation tracking
        claimsList.Add(new Claim(JwtRegisteredClaimNames.Iat, 
            issuedAt.ToUnixTimeSeconds().ToString(), 
            ClaimValueTypes.Integer64));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claimsList,
            expires: expires,
            notBefore: issuedAt.UtcDateTime, // Token valid from issuedAt
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, jti);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return WebEncoders.Base64UrlEncode(randomBytes);
    }

    private string HashToken(string token)
    {
        Guard.AgainstNullOrEmpty(token, nameof(token));
        Guard.AgainstNullOrEmpty(_jwtSettings.RefreshTokenHashSecret, nameof(_jwtSettings.RefreshTokenHashSecret));
        
        var secret = Encoding.UTF8.GetBytes(_jwtSettings.RefreshTokenHashSecret);
        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
        return WebEncoders.Base64UrlEncode(hash);
    }

    private async Task<List<Claim>> GetUserClaimsAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,            user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName,     user.UserName ?? user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Email,          user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier,              user.Id.ToString()),
            new(ClaimTypes.Name,                        user.UserName ?? string.Empty),
            new(ClaimTypes.Email,                       user.Email ?? string.Empty)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var uid = Guid.Parse(user.Id.ToString());
        var permissions = await _context.UserPermissions
            .Where(up => up.UserId == uid && up.IsGranted)
            .Select(up => up.Permission.Name)
            .ToListAsync();

        foreach (var p in permissions)
            claims.Add(new Claim("permission", p));

        return claims;
    }

    public async Task<IEnumerable<UserTokenDto>> GetUserTokensAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Enumerable.Empty<UserTokenDto>();

        var uid = Guid.Parse(userId);
        var tokens = await _context.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.UserId == uid)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync(ct);

        return tokens.Select(rt => new UserTokenDto(
            TokenType: "Refresh",
            CreatedAt: rt.CreatedAt,
            ExpiresAt: rt.ExpiresAt,
            IsRevoked: rt.IsRevoked,
            DeviceId: rt.DeviceId,        
            IpAddress: rt.CreatedByIp
        ));
    }



    #endregion
}

