
using DigiTekShop.Contracts.Interfaces.Caching;
using DigiTekShop.Infrastructure.Caching;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace DigiTekShop.Infrastructure.DependencyInjection;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var redisCs = config.GetConnectionString("Redis")
                      ?? throw new InvalidOperationException("Missing ConnectionStrings:Redis");

        // 1) Redis multiplexer (Singleton)
        var mux = ConnectionMultiplexer.Connect(redisCs);
        services.AddSingleton<IConnectionMultiplexer>(mux);

        // 2) IDistributedCache (Redis)
        services.AddStackExchangeRedisCache(o =>
        {
            o.Configuration = redisCs;
            o.InstanceName = config["RedisCache:InstanceName"] ?? "digitek:";
        });

        // 3) DataProtection key ring -> Redis
        services.AddDataProtection()
            .SetApplicationName("DigiTekShop")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
            .PersistKeysToStackExchangeRedis(mux, "DataProtection-Keys");


        // 4) خدمات کش و ریت‌لیمیت
        services.AddScoped<ICacheService, DistributedCacheService>();
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();

        // 5) (اختیاری) HealthCheck
        services.AddHealthChecks().AddRedis(redisCs, name: "redis");

        return services;
    }
}