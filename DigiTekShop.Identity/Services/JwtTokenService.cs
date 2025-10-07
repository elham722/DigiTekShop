

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using DigiTekShop.Contracts.DTOs.Auth.JwtSettings;
using DigiTekShop.Contracts.DTOs.Auth.Token;
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

    public async Task<Result<TokenResponseDto>> GenerateTokensAsync(
        string userId, string? deviceId = null, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        var claims = await GetUserClaimsAsync(user);

        var now = DateTimeOffset.UtcNow;
        var accessExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var (accessToken, jti) = CreateJwtToken(claims, accessExpiresAt.UtcDateTime);

        // refresh (raw + hash)
        var refreshTokenRaw = GenerateRefreshToken();
        var refreshExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var refreshTokenHash = HashToken(refreshTokenRaw);

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
        if (existing.ExpiresAt <= DateTime.UtcNow)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.TOKEN_EXPIRED));

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user == null)
            return Result<TokenResponseDto>.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.USER_NOT_FOUND));

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // revoke قدیمی + زنجیر کردن به جدید
            existing.Revoke("Token rotated");

            var now = DateTimeOffset.UtcNow;

            // ایجاد refresh جدید (raw + hash)
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
                RefreshToken: newRefreshRaw,            // ✅ raw جدید
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

    #region Revoke

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_TOKEN));

        var refreshHash = HashToken(refreshToken);
        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == refreshHash, ct);
        if (existing == null) return Result.Success(); // idempotent: ناشناخته هم اوکی

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

            // Check if token is revoked (if JTI tracking is implemented)
            var revoked = await IsAccessTokenRevokedAsync(jti,ct);
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

    private Task<bool> IsAccessTokenRevokedAsync(string jti, CancellationToken ct = default)
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
            new(JwtRegisteredClaimNames.Sub,            user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName,     user.UserName ?? user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Email,          user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier,              user.Id.ToString()),
            new(ClaimTypes.Name,                        user.UserName ?? string.Empty),
            new(ClaimTypes.Email,                       user.Email ?? string.Empty)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var uid = Guid.Parse(user.Id.ToString()); // اگر User.Id از جنس Guid است، Parse لازم نیست.
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


    public async Task CleanupExpiredTokensAsync(CancellationToken ct = default)
    {
        var expired = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        _context.RefreshTokens.RemoveRange(expired);
        await _context.SaveChangesAsync(ct);
    }

    #endregion
}

