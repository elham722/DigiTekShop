using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
 using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DigiTekShop.Persistence.Context;

namespace DigiTekShop.API.IntegrationTests.Factories;

/// <summary>
/// Factory برای تست‌های Integration با Redis واقعی (Testcontainers)
/// </summary>
public sealed class ApiFactoryWithRedis : WebApplicationFactory<Program>, IAsyncLifetime
{
    private IContainer? _redis;
    private bool _useExternalRedis;
    private bool _useLocalhostRedis;
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

        // تلاش برای راه‌اندازی Redis Container
        try
        {
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
        catch (Exception ex)
        {
            // بررسی اینکه آیا خطا مربوط به Docker است
            var isDockerError = ex.Message.Contains("Docker", StringComparison.OrdinalIgnoreCase) ||
                               ex.Message.Contains("docker_engine", StringComparison.OrdinalIgnoreCase) ||
                               ex.Message.Contains("npipe", StringComparison.OrdinalIgnoreCase) ||
                               ex.GetType().Name.Contains("Docker", StringComparison.OrdinalIgnoreCase) ||
                               (ex.InnerException?.Message?.Contains("Docker", StringComparison.OrdinalIgnoreCase) ?? false) ||
                               (ex.InnerException?.Message?.Contains("docker_engine", StringComparison.OrdinalIgnoreCase) ?? false);

            if (isDockerError)
            {
                // Docker در دسترس نیست - استفاده از localhost:6379 به عنوان fallback
                Console.WriteLine($"Docker not available: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine("Falling back to localhost:6379");
                Console.WriteLine("Make sure Redis is running on localhost:6379, or set TEST_REDIS environment variable.");
                _useLocalhostRedis = true;
                _redis = null;
            }
            else
            {
                // خطای دیگری - دوباره throw کنیم
                throw;
            }
        }
    }

    public new async Task DisposeAsync()
    {
        // فقط اگر Container راه‌اندازی کرده بودیم، dispose کنیم
        if (!_useExternalRedis && !_useLocalhostRedis && _redis is not null)
            await _redis.DisposeAsync();
        
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // تنظیم environment variables برای تست‌ها قبل از ساخت Configuration
        // این باید قبل از UseEnvironment و ConfigureAppConfiguration باشد
        Environment.SetEnvironmentVariable("ConnectionStrings__DBConnection", 
            "Server=(localdb)\\mssqllocaldb;Database=DigiTekShop_Test;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true");
        Environment.SetEnvironmentVariable("JwtSettings__Key", 
            "TestJwtKeyForIntegrationTests_MustBeAtLeast32Characters");
        
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            // اضافه کردن appsettings.Testing.json به صورت صریح
            var basePath = Path.GetDirectoryName(typeof(ApiFactoryWithRedis).Assembly.Location);
            if (basePath is not null)
            {
                var apiProjectPath = Path.Combine(basePath, "..", "..", "..", "..", "DigiTekShop.API");
                var testingConfigPath = Path.Combine(apiProjectPath, "appsettings.Testing.json");
                if (File.Exists(testingConfigPath))
                {
                    cfg.AddJsonFile(testingConfigPath, optional: false, reloadOnChange: false);
                }
            }
            
            // خواندن TEST_REDIS از environment variable (قبل از InitializeAsync)
            var testRedis = Environment.GetEnvironmentVariable("TEST_REDIS");
            var useExternalRedis = !string.IsNullOrWhiteSpace(testRedis);

            // تعیین connection string بر اساس نوع استفاده
            string redisConnectionString;
            if (useExternalRedis)
            {
                redisConnectionString = testRedis!;
            }
            else if (_redis is not null)
            {
                try
                {
                    // پورت دینامیک Redis Container
                    var port = _redis.GetMappedPublicPort(6379);
                    redisConnectionString = $"localhost:{port},abortConnect=false,connectTimeout=5000";
                }
                catch
                {
                    // اگر container هنوز آماده نبود، از localhost استفاده می‌کنیم
                    redisConnectionString = "localhost:6379,abortConnect=false,connectTimeout=5000";
                }
            }
            else
            {
                // استفاده از localhost:6379 به عنوان fallback
                redisConnectionString = "localhost:6379,abortConnect=false,connectTimeout=5000";
            }

            // Override تنظیمات برای محیط تست
            // مهم: این باید بعد از همه configuration sources اضافه شود تا override کند
            var dict = new Dictionary<string, string?>
            {
                // اتصال Redis (هم ConnectionStrings:Redis و هم Redis:ConnectionString)
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Redis:ConnectionString"] = redisConnectionString,

                // اتصال دیتابیس (برای تست‌های Rate Limit از SQL Server استفاده نمی‌کنیم، اما باید connection string موجود باشد)
                ["ConnectionStrings:DBConnection"] = "Server=(localdb)\\mssqllocaldb;Database=DigiTekShop_Test;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true",

                // غیرفعال کردن ApiKey برای تست‌های ساده
                ["ApiKey:Enabled"] = "false",

                // تنظیمات JWT (برای تست‌ها)
                ["JwtSettings:Key"] = "TestJwtKeyForIntegrationTests_MustBeAtLeast32Characters",
                ["JwtSettings:Issuer"] = "DigiTekShop",
                ["JwtSettings:Audience"] = "DigiTekShopClient",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "30",

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

            // اضافه کردن به انتهای configuration sources برای override
            // مهم: AddInMemoryCollection به ترتیب اضافه شدن override می‌کند
            // این باید بعد از همه configuration sources باشد
            cfg.AddInMemoryCollection(dict!);
        });
        
    }
}

