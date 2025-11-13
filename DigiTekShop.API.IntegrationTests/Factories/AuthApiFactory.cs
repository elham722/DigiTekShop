using DigiTekShop.API.IntegrationTests.Fakes;
using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;
using DigiTekShop.Persistence.Context;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigiTekShop.API.IntegrationTests.Factories;

/// <summary>
/// Factory برای تست‌های Integration سیستم Auth
/// با Redis واقعی (Testcontainers) و فیک‌های SMS/Email
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private IContainer? _redis;
    private bool _useExternalRedis;
    private bool _useLocalhostRedis;
    private string? _externalRedisConnectionString;

    /// <summary>
    /// فیک SMS برای ذخیره و استخراج OTP
    /// </summary>
    public SmsFake SmsFake { get; } = new();

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
        // چک کردن متغیر محیطی TEST_REDIS برای استفاده از Redis خارجی
        _externalRedisConnectionString = Environment.GetEnvironmentVariable("TEST_REDIS");
        _useExternalRedis = !string.IsNullOrWhiteSpace(_externalRedisConnectionString);

        if (_useExternalRedis)
        {
            Console.WriteLine($"Using external Redis: {_externalRedisConnectionString}");
            return;
        }

        // تلاش برای راه‌اندازی Redis Container
        try
        {
            _redis = new ContainerBuilder()
                .WithImage("redis:7-alpine")
                .WithCleanUp(true)
                .WithName($"dts-auth-test-redis-{Guid.NewGuid():N}")
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
                // Docker در دسترس نیست - استفاده از localhost:6379
                Console.WriteLine($"Docker not available: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine("Falling back to localhost:6379");
                Console.WriteLine("Make sure Redis is running on localhost:6379, or set TEST_REDIS environment variable.");
                _useLocalhostRedis = true;
                _redis = null;
            }
            else
            {
                throw;
            }
        }
    }

    public new async Task DisposeAsync()
    {
        if (!_useExternalRedis && !_useLocalhostRedis && _redis is not null)
            await _redis.DisposeAsync();
        
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // تنظیم environment variables برای تست‌ها
        Environment.SetEnvironmentVariable("ConnectionStrings__DBConnection", 
            "Server=(localdb)\\mssqllocaldb;Database=DigiTekShop_AuthTest;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true");
        Environment.SetEnvironmentVariable("JwtSettings__Key", 
            "TestJwtKeyForAuthIntegrationTests_MustBeAtLeast32Characters_123456");
        
        builder.UseEnvironment("Testing");
        
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            // خواندن TEST_REDIS از environment variable
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
                    var port = _redis.GetMappedPublicPort(6379);
                    redisConnectionString = $"localhost:{port},abortConnect=false,connectTimeout=5000";
                }
                catch
                {
                    redisConnectionString = "localhost:6379,abortConnect=false,connectTimeout=5000";
                }
            }
            else
            {
                redisConnectionString = "localhost:6379,abortConnect=false,connectTimeout=5000";
            }

            // Override تنظیمات برای محیط تست
            var dict = new Dictionary<string, string?>
            {
                // اتصال Redis
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Redis:ConnectionString"] = redisConnectionString,

                // اتصال دیتابیس
                ["ConnectionStrings:DBConnection"] = "Server=(localdb)\\mssqllocaldb;Database=DigiTekShop_AuthTest;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true",

                // غیرفعال کردن ApiKey
                ["ApiKey:Enabled"] = "false",

                // تنظیمات JWT
                ["JwtSettings:Key"] = "TestJwtKeyForAuthIntegrationTests_MustBeAtLeast32Characters_123456",
                ["JwtSettings:Issuer"] = "DigiTekShop",
                ["JwtSettings:Audience"] = "DigiTekShopClient",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "30",

                // تنظیمات Rate Limit - برای تست‌ها بالا می‌بریم
                ["RateLimit:WindowSeconds"] = WindowSeconds.ToString(),
                ["RateLimit:Limit"] = Limit.ToString(),

                // تنظیمات Phone Verification (OTP)
                ["PhoneVerification:WindowSeconds"] = WindowSeconds.ToString(),
                ["PhoneVerification:MaxSendPerWindow"] = Limit.ToString(),
                ["PhoneVerification:MaxVerifyPerWindow"] = Limit.ToString(),
                ["PhoneVerification:VerifyWindowSeconds"] = WindowSeconds.ToString(),
                ["PhoneVerification:CodeLength"] = "6",
                ["PhoneVerification:CodeValidity"] = "00:05:00", // ۵ دقیقه
                ["PhoneVerification:MaxAttempts"] = "3",
                ["PhoneVerification:ResendCooldown"] = "00:01:00", // ۱ دقیقه
                ["PhoneVerification:AllowResendCode"] = "true",
                ["PhoneVerification:RequirePhoneConfirmation"] = "false", // برای تست‌ها راحت‌تر
                
                // تنظیمات Lockout
                ["Lockout:MaxFailedAccessAttempts"] = "5",
                ["Lockout:DefaultLockoutTimeSpan"] = "00:05:00",
                
                // تنظیمات Device
                ["Device:MaxActiveDevicesPerUser"] = "5",
                ["Device:MaxTrustedDevicesPerUser"] = "3",
            };

            cfg.AddInMemoryCollection(dict!);
        });

        builder.ConfigureServices(services =>
        {
            // جایگزینی IPhoneSender با SmsFake
            services.RemoveAll<IPhoneSender>();
            services.AddSingleton<IPhoneSender>(SmsFake);

            // (اختیاری) اگر Email sender هم داری می‌تونی اینجا فیکش کنی
            // services.RemoveAll<IEmailSender>();
            // services.AddSingleton<IEmailSender>(EmailFake);
        });
    }
}

