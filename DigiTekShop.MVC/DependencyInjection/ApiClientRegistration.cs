using System.Net;
using DigiTekShop.MVC.Handlers;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

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


        services.AddSingleton(new CookieContainer()); // share container

        services.AddHttpClient("ApiRaw", (sp, http) =>
            {
                var opt = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
                http.BaseAddress = new Uri(opt.BaseAddress.TrimEnd('/') + "/");
                http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler((sp) =>
            {
                return new SocketsHttpHandler
                {
                    UseCookies = true,
                    CookieContainer = sp.GetRequiredService<CookieContainer>(),
                    AutomaticDecompression = DecompressionMethods.All,
                    ConnectTimeout = TimeSpan.FromSeconds(10),
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<ITokenStore, CookieClaimsTokenStore>();

        services.AddTransient<CorrelationHandler>();
        services.AddTransient<BearerTokenHandler>();
        services.AddTransient<AutoRefreshTokenHandler>();
        services.AddTransient<DiagnosticsHandler>();

        var optSnap = cfg.GetSection("ApiClient").Get<ApiClientOptions>() ?? new ApiClientOptions();
        var rng = new Random();

        IAsyncPolicy<HttpResponseMessage> retryPolicy =
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r =>
                    r.StatusCode == HttpStatusCode.TooManyRequests || // 429
                    r.StatusCode == HttpStatusCode.RequestTimeout)    // 408
                .WaitAndRetryAsync(
                    retryCount: optSnap.RetryCount,
                    sleepDurationProvider: attempt =>
                    {
                        var baseDelay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt));
                        var jitter = TimeSpan.FromMilliseconds(rng.Next(0, 200));
                        return baseDelay + jitter;
                    });

        IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy =
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: optSnap.CircuitBreakErrors,
                    durationOfBreak: TimeSpan.FromSeconds(optSnap.CircuitDurationSeconds));


        services.AddHttpClient<IApiClient, ApiClient>((sp, http) =>
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
            .AddHttpMessageHandler<DiagnosticsHandler>()
            .AddPolicyHandler((req) => req.Method == HttpMethod.Get ? retryPolicy : Policy.NoOpAsync<HttpResponseMessage>())
            .AddPolicyHandler(circuitBreakerPolicy)
            .ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler
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




        return services;
    }
}
