using StackExchange.Redis;
using DigiTekShop.Contracts.Abstractions.Caching;

namespace DigiTekShop.Infrastructure.Caching;

public sealed class RedisLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _mux;

    private const string UnlockScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end";

    public RedisLockService(IConnectionMultiplexer mux) => _mux = mux;

    public async Task<string?> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        var token = $"{Environment.MachineName}:{Guid.NewGuid():N}";
        var db = _mux.GetDatabase();
        var ok = await db.StringSetAsync(key, token, ttl, when: When.NotExists);
        return ok ? token : null;
    }

    public async Task ReleaseAsync(string key, string lockToken, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        await db.ScriptEvaluateAsync(
            UnlockScript,
            keys: new RedisKey[] { key },
            values: new RedisValue[] { lockToken }
        );
    }
}