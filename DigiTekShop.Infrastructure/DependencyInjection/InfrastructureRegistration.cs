
using DigiTekShop.Contracts.Abstractions.Caching;
using DigiTekShop.Contracts.Abstractions.Events;
using DigiTekShop.Contracts.Abstractions.Time;
using DigiTekShop.Infrastructure.Caching;
using DigiTekShop.Infrastructure.Events;
using DigiTekShop.Infrastructure.Time;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace DigiTekShop.Infrastructure.DependencyInjection;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config,
        IHostEnvironment env) 
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

        // 3) DataProtection: Dev = فایل / Prod = Redis
        var dp = services.AddDataProtection()
            .SetApplicationName("DigiTekShop")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

        if (env.IsDevelopment())
        {
            var keysPath = Path.Combine(AppContext.BaseDirectory, "dp-keys");
            dp.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
        else
        {
            dp.PersistKeysToStackExchangeRedis(mux, "DataProtection-Keys");
        }

        // 4) خدمات کش و ریت‌لیمیت
        services.AddScoped<ICacheService, DistributedCacheService>();
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();
        services.AddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();

               // DomainEvent Publisher (in-process MediatR)
               services.AddScoped<IDomainEventPublisher, MediatRDomainEventPublisher>();

               // Outbox pattern for reliable messaging
               services.AddScoped<IOutboxEventRepository, InfrastructureOutboxEventRepository>();
               services.AddHostedService<OutboxEventProcessor>();

        // 5) HealthCheck
        services.AddHealthChecks().AddRedis(redisCs, name: "redis");


        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<IDistributedLockService, RedisLockService>();


        return services;
    }
}
