using System.Net;
using DigiTekShop.MVC.Handlers;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace DigiTekShop.MVC.Services;

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

        services.AddHttpContextAccessor();

        services.AddTransient<CorrelationHandler>();
        services.AddTransient<BearerTokenHandler>();
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
            http.DefaultRequestVersion = HttpVersion.Version20;
            http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DigiTekShop.MVC/1.0");
        })
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(circuitBreakerPolicy)
        .AddHttpMessageHandler<CorrelationHandler>()
        .AddHttpMessageHandler<BearerTokenHandler>()
        .AddHttpMessageHandler<DiagnosticsHandler>()
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            MaxConnectionsPerServer = 50,
            EnableMultipleHttp2Connections = true,
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, cert, chain, errors)
                    => env.IsDevelopment() ? true : errors == System.Net.Security.SslPolicyErrors.None
            }
        });

        return services;
    }
}
