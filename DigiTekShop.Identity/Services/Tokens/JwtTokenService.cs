

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Interfaces.Caching;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Exceptions.Common;
using DigiTekShop.Identity.Models;
using DigiTekShop.Identity.Options;
using DigiTekShop.Identity.Options.Security;
using DigiTekShop.SharedKernel.Enums;
using DigiTekShop.SharedKernel.Exceptions.Common;
using DigiTekShop.SharedKernel.Exceptions.Validation;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

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
            catch (InvalidDomainOperationException ex) when (ex.ErrorCode == IdentityErrorCodes.MaxActiveDevicesExceeded)
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
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

        var refreshHash = HashToken(refreshToken);

        var existing = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshHash,ct);

        if (existing == null)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_NOT_FOUND));
        if (existing.IsRevoked)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_REVOKED));
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
            
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_ALREADY_USED), IdentityErrorCodes.TOKEN_ALREADY_USED);
        }
        if (existing.ExpiresAt <= DateTime.UtcNow)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_EXPIRED));

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user == null)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
           
            await ValidateTokenRotationSecurityAsync(existing, ct);

           
            existing.MarkAsUsed();

            existing.Revoke("Token rotated");

            var now = DateTimeOffset.UtcNow;

            var newRefreshRaw = GenerateRefreshToken();
            var newRefreshHash = HashToken(newRefreshRaw);
            var newRefreshExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays);

            var newRefresh = RefreshToken.Create(
                tokenHash: newRefreshHash,
                expiresAt: newRefreshExpiresAt.UtcDateTime,
                userId: existing.UserId,
                deviceId: deviceId,
                ip: ipAddress,
                userAgent: userAgent,
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
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Error rotating refresh token");
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));
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

    
    private async Task RevokeTokenChainAsync(RefreshToken token, string reason, CancellationToken ct = default)
    {
        var tokensToRevoke = new List<RefreshToken> { token };

        
        var childTokens = await _context.RefreshTokens
            .Where(rt => rt.ParentTokenHash == token.TokenHash && !rt.IsRevoked)
            .ToListAsync(ct);

        tokensToRevoke.AddRange(childTokens);

      
        if (!string.IsNullOrEmpty(token.ParentTokenHash))
        {
            var parentToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == token.ParentTokenHash && !rt.IsRevoked, ct);
            
            if (parentToken != null)
            {
                tokensToRevoke.Add(parentToken);
            }
        }

        foreach (var t in tokensToRevoke)
        {
            t.Revoke(reason);
        }

        if (tokensToRevoke.Any())
        {
            _context.RefreshTokens.UpdateRange(tokensToRevoke);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Revoked token chain with {Count} tokens for security violation", tokensToRevoke.Count);
        }
    }

    #endregion

    #region Revoke

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

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
        if (string.IsNullOrWhiteSpace(userId)) return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        if (!Guid.TryParse(userId, out var userGuid))
            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

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
            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

        try
        {
           
            var tokenHandler = new JwtSecurityTokenHandler();
            
            if (!tokenHandler.CanReadToken(accessToken))
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

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
        if (string.IsNullOrWhiteSpace(accessToken)) return Result<ClaimsPrincipal>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

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
                return Result<ClaimsPrincipal>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

            // Check if token is revoked (JTI blacklist)
            var isJtiRevoked = await _tokenBlacklist.IsTokenRevokedAsync(jti, ct);
            if (isJtiRevoked)
            {
                _logger.LogWarning("Access denied: Blacklisted token JTI {Jti}", jti);
                return Result<ClaimsPrincipal>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_ALREADY_REVOKED));
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
                    return Result<ClaimsPrincipal>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_ALREADY_REVOKED));
                }
            }

            return Result<ClaimsPrincipal>.Success(principal);
        }
        catch (SecurityTokenExpiredException)
        {
            return Result<ClaimsPrincipal>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_EXPIRED));
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("Invalid token signature");
            return Result<ClaimsPrincipal>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return Result<ClaimsPrincipal>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));
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

