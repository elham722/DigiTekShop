
using DigiTekShop.Application.Common.Messaging;
using DigiTekShop.Contracts.Abstractions.Caching;
using DigiTekShop.Infrastructure.Background;
using DigiTekShop.Infrastructure.Caching;
using DigiTekShop.Infrastructure.DomainEvents;
using DigiTekShop.Infrastructure.Messaging;
using DigiTekShop.Infrastructure.Time;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Time;
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
        services.AddSingleton<ICacheService, DistributedCacheService>();
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();
        services.AddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();

         

        // 5) HealthCheck
        services.AddHealthChecks().AddRedis(redisCs, name: "redis");


        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<IDistributedLockService, RedisLockService>();


        // Bus (فعلاً لاگ؛ بعداً Rabbit/Kafka/Redis جایگزین کن)
        services.AddSingleton<IMessageBus, LoggingMessageBus>();

        // Workers
        services.AddHostedService<ShopOutboxPublisherService>();
        services.AddHostedService<IdentityOutboxPublisherService>();

        services.AddScoped<IDomainEventSink, DomainEventSink>();

        return services;
    }
}
