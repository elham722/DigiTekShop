using System.Net;
using DigiTekShop.MVC.Handlers;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace DigiTekShop.MVC.DependencyInjection;

public static class ApiClientRegistration
{
    public static IServiceCollection AddDigiTekShopApiClient(
        this IServiceCollection services,
        IConfiguration cfg,
        IHostEnvironment env)
    {
        services.AddOptions<ApiClientOptions>()
            .Bind(cfg.GetSection("ApiClient"))
            .Validate(o => Uri.TryCreate(o.BaseAddress, UriKind.Absolute, out _), "Invalid BaseAddress")
            .Validate(o =>
            {
                var u = new Uri(o.BaseAddress);
                return env.IsDevelopment() || u.Scheme == Uri.UriSchemeHttps;
            }, "HTTPS required in non-Development")
            .Validate(o => o.TimeoutSeconds is >= 1 and <= 120, "Timeout out of range (1-120)")
            .ValidateOnStart();

        services.AddSingleton(new CookieContainer());

        services.AddHttpClient("ApiRaw", (sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
            http.BaseAddress = new Uri(opt.BaseAddress.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
        })
            .ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler
            {
                UseCookies = true,
                CookieContainer = sp.GetRequiredService<CookieContainer>(),
                AutomaticDecompression = DecompressionMethods.All,
                ConnectTimeout = TimeSpan.FromSeconds(10),
            });

        services.AddHttpContextAccessor();
        services.AddScoped<ITokenStore, CookieClaimsTokenStore>();

        services.AddTransient<CorrelationHandler>();
        services.AddTransient<BearerTokenHandler>();
        services.AddTransient<AutoRefreshTokenHandler>();
        services.AddTransient<DiagnosticsHandler>();

        var optSnap = cfg.GetSection("ApiClient").Get<ApiClientOptions>() ?? new ApiClientOptions();
        var rng = new Random();

        var httpBuilder = services.AddHttpClient<IApiClient, ApiClient>((sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
            http.BaseAddress = new Uri(opt.BaseAddress.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DigiTekShop.MVC/1.0");
        })
            .AddHttpMessageHandler<CorrelationHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler<AutoRefreshTokenHandler>()
            .AddHttpMessageHandler<DiagnosticsHandler>();

        httpBuilder.ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler
        {
            UseCookies = true,
            CookieContainer = sp.GetRequiredService<CookieContainer>(),
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 50,
            EnableMultipleHttp2Connections = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
        });

        httpBuilder.AddResilienceHandler("api", builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = optSnap.RetryCount,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false, 
                ShouldHandle = args =>
                {
                    var resp = args.Outcome.Result;
                    var ex = args.Outcome.Exception;

                    var isGet = resp?.RequestMessage?.Method == HttpMethod.Get;

                    var isTransient =
                        (resp is not null && (
                            resp.StatusCode == HttpStatusCode.TooManyRequests ||
                            resp.StatusCode == HttpStatusCode.RequestTimeout ||
                            (int)resp.StatusCode >= 500
                        )) || ex is not null;

                    return new ValueTask<bool>(isGet && isTransient);
                },
                DelayGenerator = args =>
                {
                    
                    var ra = args.Outcome.Result?.Headers?.RetryAfter?.Delta;
                    if (ra is { TotalMilliseconds: > 0 })
                        return new ValueTask<TimeSpan?>(ra);

                    var baseDelay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, args.AttemptNumber));
                    var jitter = TimeSpan.FromMilliseconds(rng.Next(0, 200));
                    return new ValueTask<TimeSpan?>(baseDelay + jitter);
                }
            });

            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5, 
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = Math.Max(2, optSnap.CircuitBreakErrors),
                BreakDuration = TimeSpan.FromSeconds(optSnap.CircuitDurationSeconds),
                ShouldHandle = args =>
                {
                    var resp = args.Outcome.Result;
                    var ex = args.Outcome.Exception;

                    var isTransient =
                        (resp is not null && (
                            (int)resp.StatusCode >= 500 ||
                            resp.StatusCode == HttpStatusCode.RequestTimeout ||
                            resp.StatusCode == HttpStatusCode.TooManyRequests
                        )) || ex is not null;

                    return new ValueTask<bool>(isTransient);
                }
            });
        });

        return services;
    }
}
