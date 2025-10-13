using StackExchange.Redis;
using DigiTekShop.Contracts.Abstractions.Caching;

namespace DigiTekShop.Infrastructure.Caching;

public class RedisLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _mux;
    public RedisLockService(IConnectionMultiplexer mux) => _mux = mux;

    public async Task<bool> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        
        return await db.StringSetAsync(
            key,
            Environment.MachineName,   
            ttl,
            when: When.NotExists);
    }

    public async Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}