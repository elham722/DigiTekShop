using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Options.Token;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace DigiTekShop.Identity.Services.Tokens;

public sealed class TokenService : ITokenService
{
    private readonly UserManager<User> _users;
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IDateTimeProvider _time;
    private readonly JwtSettings _jwt;
    private readonly ITokenBlacklistService _blacklist;
    private readonly ICurrentClient _client;
    private readonly ILogger<TokenService> _log;

    private static class Events
    {
        public static readonly EventId Issue = new(30001, nameof(IssueAsync));
        public static readonly EventId Refresh = new(30002, nameof(RefreshAsync));
        public static readonly EventId Revoke = new(30003, nameof(RevokeAsync));
        public static readonly EventId RevokeAll = new(30004, nameof(RevokeAllAsync));
        public static readonly EventId RevokeJti = new(30005, nameof(RevokeAccessJtiAsync));
    }

    public TokenService(
        UserManager<User> users,
        DigiTekShopIdentityDbContext db,
        IDateTimeProvider time,
        IOptions<JwtSettings> jwt,
        ITokenBlacklistService blacklist,
        ICurrentClient client,
        ILogger<TokenService> log)
    {
        _users = users;
        _db = db;
        _time = time;
        _jwt = jwt.Value;
        _blacklist = blacklist;
        _client = client;
        _log = log;
    }

    public async Task<Result<RefreshTokenResponse>> IssueAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<RefreshTokenResponse>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var (access, accessExp, accessIat, jti) = CreateAccessToken(user);
        var (rawRefresh, refreshHash, refreshExp) = CreateRefreshToken();

        var rt = RefreshToken.Create(
            tokenHash: refreshHash,
            expiresAtUtc: refreshExp,
            userId: user.Id,
            deviceId: _client.DeviceId,
            createdByIp: _client.IpAddress,
            userAgent: _client.UserAgent,
            parentTokenHash: null,
            createdAtUtc: _time.UtcNow);

        
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(Events.Issue, "Issued tokens for user={UserId}", user.Id);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse
        {
            AccessToken = access,
            RefreshToken = rawRefresh,
            TokenType = "Bearer",
            ExpiresIn = (int)(_jwt.AccessTokenExpirationMinutes * 60),
            IssuedAtUtc = accessIat.UtcDateTime,
            ExpiresAtUtc = accessExp.UtcDateTime
        });
    }

    public async Task<Result<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return Result<RefreshTokenResponse>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var now = _time.UtcNow;
        var hash = HashRefreshToken(dto.RefreshToken);

        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
      
        if (!string.IsNullOrEmpty(token.ReplacedByTokenHash))
        {
            var actives = await _db.RefreshTokens
                .Where(t => t.UserId == token.UserId && !t.IsRevoked && t.ExpiresAtUtc > now)
                .ToListAsync(ct);
            foreach (var t in actives) t.Revoke("reuse_detected", now);
            await _db.SaveChangesAsync(ct);

            _log.LogWarning(Events.Refresh, "Refresh reuse detected. user={UserId}", token.UserId);
            return Result<RefreshTokenResponse>.Failure(ErrorCodes.Identity.TOKEN_REVOKED);
        }


        if (token is null || token.User is null)
            return Result<RefreshTokenResponse>.Failure(ErrorCodes.Identity.INVALID_TOKEN);

        if (!token.IsActive)
            return Result<RefreshTokenResponse>.Failure(ErrorCodes.Identity.TOKEN_REVOKED);

        
        token.MarkAsUsed(now);
        token.Revoke("rotated", now);

        var (access, accessExp, accessIat, jti) = CreateAccessToken(token.User);
        var (rawRefresh, newHash, newExp) = CreateRefreshToken();

        token.MarkAsRotated(newHash, now);

        var replacement = RefreshToken.Create(
            tokenHash: newHash,
            expiresAtUtc: newExp,
            userId: token.UserId,
            deviceId: _client.DeviceId,
            createdByIp: _client.IpAddress,
            userAgent: _client.UserAgent,
            parentTokenHash: token.TokenHash,
            createdAtUtc: now);

        _db.RefreshTokens.Add(replacement);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _log.LogWarning(Events.Refresh, ex, "Concurrency on refresh rotation. user={UserId}", token.UserId);
            return Result<RefreshTokenResponse>.Failure(ErrorCodes.Common.CONCURRENCY_CONFLICT);
        }

        _log.LogInformation(Events.Refresh, "Rotated refresh token for user={UserId}", token.UserId);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse
        {
            AccessToken = access,
            RefreshToken = rawRefresh,
            TokenType = "Bearer",
            ExpiresIn = (int)(_jwt.AccessTokenExpirationMinutes * 60),
            IssuedAtUtc = accessIat.UtcDateTime,
            ExpiresAtUtc = accessExp.UtcDateTime
        });
    }

    public async Task<Result> RevokeAsync(string? refreshToken, Guid userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var hash = HashRefreshToken(refreshToken);
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, ct);

        if (token is null) return Result.Success(); 

        if (!token.IsRevoked)
        {
            token.Revoke("manual", _time.UtcNow);
            await _db.SaveChangesAsync(ct);
        }

        _log.LogInformation(Events.Revoke, "Revoked refresh token for user={UserId}", userId);
        return Result.Success();
    }

    public async Task<Result> RevokeAllAsync(Guid userId, CancellationToken ct)
    {
        var now = _time.UtcNow;
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAtUtc > now)
            .ToListAsync(ct);

        foreach (var t in active) t.Revoke("global_logout", now);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(Events.RevokeAll, "Revoked all active refresh tokens. user={UserId}", userId);
        return Result.Success();
    }

    public async Task<Result> RevokeAccessJtiAsync(string jti, CancellationToken ct)
    {
        var expiresAt = _time.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);
        await _blacklist.RevokeAccessTokenAsync(jti, expiresAt, "manual", ct);
        _log.LogInformation(Events.RevokeJti, "Blacklisted access token jti={Jti}", jti);
        return Result.Success();
    }

    #region Helpers

    private (string token, DateTimeOffset expiresAt, DateTimeOffset issuedAt, string jti)
       CreateAccessToken(User user)
    {
        var now = _time.UtcNow;
        var expires = now.AddMinutes(_jwt.AccessTokenExpirationMinutes);
        var jti = Guid.NewGuid().ToString("N");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat, ToUnix(now).ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(_client.DeviceId))
            claims.Add(new("did", _client.DeviceId!));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,    
            expires: expires,
            signingCredentials: creds);

        var raw = new JwtSecurityTokenHandler().WriteToken(token);
        return (raw, expires, now, jti);
    }

    private (string rawToken, string hash, DateTimeOffset expiresAt) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var raw = Base64Url(bytes);
        var expires = _time.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays);
        var hash = HashRefreshToken(raw);
        return (raw, hash, expires);
    }

    private string HashRefreshToken(string raw)
    {
        var secret = _jwt.RefreshTokenHashSecret;
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("RefreshTokenHashSecret not set in JwtSettings.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Base64Url(bytes);
    }

    private static string Base64Url(ReadOnlySpan<byte> bytes)
    {
        var s = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return s;
    }

    private static long ToUnix(DateTimeOffset dt) => (long)(dt - DateTimeOffset.UnixEpoch).TotalSeconds;

    #endregion


}
