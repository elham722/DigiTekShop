using DigiTekShop.Contracts.DTOs.Cache;
using DigiTekShop.Contracts.Interfaces.Caching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace DigiTekShop.Infrastructure.Caching;


public sealed class RedisTokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTokenBlacklistService> _logger;
    private readonly IDatabase _db;

    // Redis key prefixes
    private const string JTI_BLACKLIST_PREFIX = "tokenbl:jti:";
    private const string USER_REVOCATION_PREFIX = "tokenbl:user:";

    public RedisTokenBlacklistService(
        IConnectionMultiplexer redis,
        ILogger<RedisTokenBlacklistService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = _redis.GetDatabase();
    }

    /// <inheritdoc/>
    public async Task RevokeAccessTokenAsync(string jti, DateTime expiresAt, string? reason = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            throw new ArgumentException("JTI cannot be null or empty", nameof(jti));

        try
        {
            var key = GetJtiKey(jti);
            var ttl = CalculateTTL(expiresAt);

            if (ttl <= TimeSpan.Zero)
            {
                _logger.LogWarning("Attempted to revoke already expired token JTI: {Jti}", jti);
                return;
            }

            var data = new TokenRevocationData
            {
                Jti = jti,
                RevokedAt = DateTime.UtcNow,
                Reason = reason ?? "Token revoked",
                ExpiresAt = expiresAt
            };

            var json = JsonSerializer.Serialize(data);
            await _db.StringSetAsync(key, json, ttl);

            _logger.LogInformation("Access token revoked: JTI={Jti}, Reason={Reason}, TTL={Ttl}",
                jti, reason, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking access token: JTI={Jti}", jti);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsTokenRevokedAsync(string jti, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return false;

        try
        {
            var key = GetJtiKey(jti);
            var exists = await _db.KeyExistsAsync(key);
            
            if (exists)
            {
                _logger.LogWarning("Revoked token access attempt: JTI={Jti}", jti);
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token revocation: JTI={Jti}", jti);
            // در صورت خطا، از سمت امنیت، فرض می‌کنیم توکن revoke شده
            // اما می‌تونید false هم برگردونید تا سرویس از کار نیفته
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task RevokeAllUserTokensAsync(Guid userId, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            var key = GetUserRevocationKey(userId);
            var timestamp = DateTime.UtcNow;

            var data = new UserRevocationData
            {
                UserId = userId,
                RevokedAt = timestamp,
                Reason = reason ?? "All user tokens revoked"
            };

            var json = JsonSerializer.Serialize(data);
            
            // این key هیچ TTL نداره چون باید تا ابد بمونه (یا دستی پاک بشه)
            // در عمل می‌تونید TTL طولانی بزارید (مثلاً 90 روز)
            await _db.StringSetAsync(key, json, TimeSpan.FromDays(90));

            _logger.LogWarning("All tokens revoked for user: UserId={UserId}, Reason={Reason}",
                userId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all user tokens: UserId={UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsUserTokensRevokedAsync(Guid userId, DateTime tokenIssuedAt, CancellationToken ct = default)
    {
        try
        {
            var key = GetUserRevocationKey(userId);
            var json = await _db.StringGetAsync(key);

            if (json.IsNullOrEmpty)
                return false;

            var data = JsonSerializer.Deserialize<UserRevocationData>(json!);
            if (data == null)
                return false;

            // اگر توکن قبل از revocation timestamp صادر شده، invalid است
            var isRevoked = tokenIssuedAt < data.RevokedAt;

            if (isRevoked)
            {
                _logger.LogWarning("User-level revoked token access attempt: UserId={UserId}, TokenIssuedAt={TokenIssuedAt}, RevokedAt={RevokedAt}",
                    userId, tokenIssuedAt, data.RevokedAt);
            }

            return isRevoked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user-level token revocation: UserId={UserId}", userId);
            return false;
        }
    }

    #region Private Helpers

    private static string GetJtiKey(string jti) => $"{JTI_BLACKLIST_PREFIX}{jti}";
    private static string GetUserRevocationKey(Guid userId) => $"{USER_REVOCATION_PREFIX}{userId}";

    private static TimeSpan CalculateTTL(DateTime expiresAt)
    {
        var ttl = expiresAt - DateTime.UtcNow;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
    }

    #endregion

}

