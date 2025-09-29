

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using DigiTekShop.Contracts.DTOs.JwtSettings;
using DigiTekShop.Contracts.Interfaces.Identity;
using DigiTekShop.Identity.Exceptions.Common;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DigiTekShop.Identity.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly UserManager<User> _userManager;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;


    public JwtTokenService(
        UserManager<User> userManager,
        DigiTekShopIdentityDbContext context,
        IOptions<JwtSettings> jwtOptions,
        ILogger<JwtTokenService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jwtSettings = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Generate / Create tokens

    public async Task<Result<TokenResponseDto>> GenerateTokensAsync(string userId, string? deviceId = null, string? ipAddress = null, string? userAgent = null)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        var claims = await GetUserClaimsAsync(user);
        var accessExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Create access token (with jti)
        var (accessToken, jti) = CreateJwtToken(claims, accessExpires);

        // Create secure refresh token (raw)
        var refreshTokenRaw = GenerateRefreshToken();
        var refreshExpires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var refreshTokenHash = HashToken(refreshTokenRaw);

        var refreshEntity = RefreshToken.Create(refreshTokenHash, refreshExpires, userGuid, deviceId, ipAddress, userAgent);
    
        _context.RefreshTokens.Add(refreshEntity);

        await _context.SaveChangesAsync();

        var dto = new TokenResponseDto(accessToken, (int)(accessExpires - DateTime.UtcNow).TotalSeconds, refreshTokenRaw, refreshExpires);
        return Result<TokenResponseDto>.Success(dto);
    }

    #endregion

    #region Refresh / Rotation

    public async Task<Result<TokenResponseDto>> RefreshTokensAsync(string refreshToken, string? deviceId = null, string? ipAddress = null, string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

        var refreshHash = HashToken(refreshToken);

        // Find existing refresh token entry by hash
        var existing = await _context.RefreshTokens
            .Where(rt => rt.TokenHash == refreshHash)
            .FirstOrDefaultAsync();

        if (existing == null) return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_NOT_FOUND));
        if (existing.IsRevoked) return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_REVOKED));
        if (existing.ExpiresAt <= DateTime.UtcNow) return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_EXPIRED));

        // Load user
        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user == null) return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        // Begin rotation transaction
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Mark existing token as rotated
            existing.MarkAsRotated(refreshHash);
            existing.Revoke("Token rotated");
            _context.RefreshTokens.Update(existing);

            // Create new refresh token with parent relationship
            var newRefreshRaw = GenerateRefreshToken();
            var newRefreshHash = HashToken(newRefreshRaw);
            var newRefresh = RefreshToken.Create(newRefreshHash, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                existing.UserId, deviceId, ipAddress, userAgent, existing.TokenHash);
 
            _context.RefreshTokens.Add(newRefresh);

            // Create new access token
            var claims = await GetUserClaimsAsync(user);
            var accessExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            var (accessToken, jti) = CreateJwtToken(claims, accessExpires);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            var dto = new TokenResponseDto(accessToken, (int)(accessExpires - DateTime.UtcNow).TotalSeconds, newRefreshRaw, newRefresh.ExpiresAt);
            return Result<TokenResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error rotating refresh token");
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));
        }
    }

    #endregion

    #region Revoke

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

        var refreshHash = HashToken(refreshToken);
        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == refreshHash);
        if (existing == null) return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_NOT_FOUND));

        if (existing.IsRevoked) return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_ALREADY_REVOKED));

        existing.Revoke("RevokedByUser");
        _context.RefreshTokens.Update(existing);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> RevokeAllUserTokensAsync(string userId, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        if (!Guid.TryParse(userId, out var userGuid))
            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        var tokens = await _context.RefreshTokens.Where(rt => rt.UserId == userGuid && !rt.IsRevoked).ToListAsync();
        if (!tokens.Any()) return Result.Success();

        foreach (var t in tokens)
        {
            t.Revoke(reason ?? "Revoked all user tokens");
        }

        _context.RefreshTokens.UpdateRange(tokens);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    #endregion

    #region Validate Access Token

    public async Task<Result<ClaimsPrincipal>> ValidateAccessTokenAsync(string accessToken)
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

            // Check if token is revoked (if JTI tracking is implemented)
            var revoked = await IsAccessTokenRevokedAsync(jti);
            if (revoked) return Result<ClaimsPrincipal>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_ALREADY_REVOKED));

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

    private Task<bool> IsAccessTokenRevokedAsync(string jti)
    {
        // Placeholder:
        // If you implement storing access-jti in DB, check it here (and check revoked flag).
        return Task.FromResult(false);
    }

    #endregion

    #region Helpers & Utils

    private (string token, string jti) CreateJwtToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var jti = Guid.NewGuid().ToString();
        var claimsList = claims.ToList();
        claimsList.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claimsList,
            expires: expires,
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
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        // Add roles
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

        // Add permissions from your domain model if any (example)
        var permissions = await _context.UserPermissions
            .Where(up => up.UserId == Guid.Parse(user.Id.ToString()) && up.IsGranted)
            .Select(up => up.Permission)
            .Select(p => p.Name)
            .ToListAsync();

        foreach (var p in permissions)
        {
            claims.Add(new Claim("permission", p));
        }

        return claims;
    }

    public async Task<IEnumerable<UserTokenDto>> GetUserTokensAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Enumerable.Empty<UserTokenDto>();

        var uid = Guid.Parse(userId);
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == uid)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();

        return tokens.Select(rt => new UserTokenDto(
            TokenType: "Refresh",
            CreatedAt: rt.CreatedAt,
            ExpiresAt: rt.ExpiresAt,
            IsRevoked: rt.IsRevoked,
            DeviceId: null,
            IpAddress: null));
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var expired = await _context.RefreshTokens.Where(rt => rt.ExpiresAt <= DateTime.UtcNow).ToListAsync();
        if (!expired.Any()) return;

        _context.RefreshTokens.RemoveRange(expired);
        await _context.SaveChangesAsync();
    }

    #endregion
}

