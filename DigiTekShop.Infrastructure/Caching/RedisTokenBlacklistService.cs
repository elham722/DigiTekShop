using DigiTekShop.Contracts.Abstractions.Caching;
using DigiTekShop.Contracts.DTOs.Cache;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace DigiTekShop.Infrastructure.Caching;

public sealed class RedisTokenBlacklistService : ITokenBlacklistService
{
    private const string JtiPrefix = "dts:tokenbl:jti:";
    private const string UserPrefix = "dts:tokenbl:user:";
    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTokenBlacklistService> _logger;
    private readonly IDatabase _db;

    public RedisTokenBlacklistService(IConnectionMultiplexer redis, ILogger<RedisTokenBlacklistService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = _redis.GetDatabase();
    }

    public async Task RevokeAccessTokenAsync(string jti, DateTime expiresAt, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            throw new ArgumentException("JTI cannot be null or empty", nameof(jti));

        try
        {
            var key = JtiKey(jti);
            var ttl = expiresAt - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
            {
                _logger.LogInformation("Skip revoke (already expired) JTI={Jti}", jti);
                return;
            }

            var payload = new TokenRevocationData(jti, DateTime.UtcNow, reason ?? "revoked", expiresAt);
            var json = JsonSerializer.Serialize(payload, _json);

            await _db.StringSetAsync(key, json, ttl);
            _logger.LogInformation("Access token revoked: JTI={Jti}, TTL={Ttl}", jti, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking access token: JTI={Jti}", jti);
            throw;
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string jti, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return false;

        try
        {
            var exists = await _db.KeyExistsAsync(JtiKey(jti));
            if (exists)
                _logger.LogWarning("Revoked token access attempt: JTI={Jti}", jti);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token revocation: JTI={Jti}", jti);
            
            return false;
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            var key = UserKey(userId);
            var data = new UserRevocationData(userId, DateTime.UtcNow, reason ?? "all revoked");
            var json = JsonSerializer.Serialize(data, _json);

            await _db.StringSetAsync(key, json, TimeSpan.FromDays(90));
            _logger.LogWarning("All tokens revoked for user={UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all user tokens: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsUserTokensRevokedAsync(Guid userId, DateTime tokenIssuedAt, CancellationToken ct = default)
    {
        try
        {
            var json = await _db.StringGetAsync(UserKey(userId));
            if (json.IsNullOrEmpty) return false;

            var data = JsonSerializer.Deserialize<UserRevocationData>(json!)!;
            var revoked = tokenIssuedAt < data.RevokedAt;

            if (revoked)
                _logger.LogWarning("User-level revoked token access attempt: user={UserId}, iat={Iat:o}, revokedAt={RevokedAt:o}",
                    userId, tokenIssuedAt, data.RevokedAt);

            return revoked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user-level token revocation: UserId={UserId}", userId);
            return false;
        }
    }

    private static string JtiKey(string jti) => $"{JtiPrefix}{jti}";
    private static string UserKey(Guid userId) => $"{UserPrefix}{userId}";
}
