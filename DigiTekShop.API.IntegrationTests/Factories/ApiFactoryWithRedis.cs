using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DigiTekShop.API.IntegrationTests.Factories;

/// <summary>
/// Factory برای تست‌های Integration با Redis واقعی (Testcontainers)
/// </summary>
public sealed class ApiFactoryWithRedis : WebApplicationFactory<Program>, IAsyncLifetime
{
    private IContainer? _redis;
    private bool _useExternalRedis;
    private string? _externalRedisConnectionString;

    /// <summary>
    /// تعداد درخواست‌های مجاز در پنجره (برای تست‌ها)
    /// </summary>
    public int Limit { get; } = 10;

    /// <summary>
    /// طول پنجره به ثانیه (برای تست‌ها)
    /// </summary>
    public int WindowSeconds { get; } = 60;

    public async Task InitializeAsync()
    {
        // چک کردن متغیر محیطی TEST_REDIS برای استفاده از Redis خارجی (مثلاً در VM)
        _externalRedisConnectionString = Environment.GetEnvironmentVariable("TEST_REDIS");
        _useExternalRedis = !string.IsNullOrWhiteSpace(_externalRedisConnectionString);

        if (_useExternalRedis)
        {
            // استفاده از Redis خارجی - نیازی به Container نیست
            Console.WriteLine($"Using external Redis: {_externalRedisConnectionString}");
            return;
        }

        // راه‌اندازی Redis Container (فقط اگر TEST_REDIS تنظیم نشده باشد)
        _redis = new ContainerBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .WithName($"dts-redis-it-{Guid.NewGuid():N}")
            .WithPortBinding(0, 6379) // پورت دینامیک
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("redis-cli", "ping"))
            .Build();

        await _redis.StartAsync();
        Console.WriteLine($"Started Redis container at port {_redis.GetMappedPublicPort(6379)}");
    }

    public new async Task DisposeAsync()
    {
        // فقط اگر Container راه‌اندازی کرده بودیم، dispose کنیم
        if (!_useExternalRedis && _redis is not null)
            await _redis.DisposeAsync();
        
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            // تعیین connection string بر اساس نوع استفاده (External یا Container)
            string redisConnectionString;
            if (_useExternalRedis)
            {
                redisConnectionString = _externalRedisConnectionString!;
            }
            else
            {
                // پورت دینامیک Redis Container
                var port = _redis!.GetMappedPublicPort(6379);
                redisConnectionString = $"localhost:{port},abortConnect=false,connectTimeout=5000";
            }

            // Override تنظیمات برای محیط تست
            var dict = new Dictionary<string, string?>
            {
                // اتصال Redis
                ["Redis:ConnectionString"] = redisConnectionString,

                // غیرفعال کردن ApiKey برای تست‌های ساده
                ["ApiKey:Enabled"] = "false",

                // تنظیمات Rate Limit عمومی
                ["RateLimit:WindowSeconds"] = WindowSeconds.ToString(),
                ["RateLimit:Limit"] = Limit.ToString(),

                // تنظیمات Login Flow
                ["LoginFlow:RateLimit:WindowSeconds"] = WindowSeconds.ToString(),
                ["LoginFlow:RateLimit:Limit"] = Limit.ToString(),

                // تنظیمات Phone Verification (OTP)
                ["PhoneVerification:WindowSeconds"] = WindowSeconds.ToString(),
                ["PhoneVerification:MaxSendPerWindow"] = Limit.ToString(),
                ["PhoneVerification:MaxVerifyPerWindow"] = Limit.ToString(),
                ["PhoneVerification:VerifyWindowSeconds"] = WindowSeconds.ToString(),
            };

            cfg.AddInMemoryCollection(dict!);
        });
    }
}

