using Microsoft.AspNetCore.ResponseCompression;
using Polly.Extensions.Http;
using Polly;
using System.IO.Compression;

namespace DigiTekShop.API.Extensions.Performance;

public static class PerformanceExtensions
{

    public static IServiceCollection AddResponseCompressionOptimized(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/problem+json",
                "application/xml",
                "text/plain",
                "text/css",
                "text/html",
                "application/javascript",
                "text/javascript",
                "application/manifest+json",
                "image/svg+xml"
            });
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                ? CompressionLevel.Fastest
                : CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }

    public static IServiceCollection AddOutputCachingOptimized(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {

            options.AddBasePolicy(builder => builder
                .With(ctx =>
                {
                    var http = ctx.HttpContext;
                    var path = http.Request.Path.Value ?? string.Empty;


                    if (http.Request.Method != HttpMethods.Get && http.Request.Method != HttpMethods.Head)
                        return false;


                    if (http.User?.Identity?.IsAuthenticated == true)
                        return false;


                    if (System.Text.RegularExpressions.Regex.IsMatch(path, @"^/api/v\d+/(auth|registration|password|twofactor)/", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        return false;
                    if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                        return false;
                    if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api-docs", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return true;
                })
                .Expire(TimeSpan.FromSeconds(60))
                .Tag("default")
            );


            options.AddPolicy("StaticContent", b => b
                .Expire(TimeSpan.FromHours(1))
                .Tag("static")
                .SetVaryByHeader("Accept-Encoding"));


            options.AddPolicy("ApiResponse", b => b
                .Expire(TimeSpan.FromMinutes(5))
                .Tag("api")
                .SetVaryByQuery("page", "pageSize", "sort"));
            // .SetVaryByHeader("Accept-Language") 
        });

        return services;
    }


    public static IServiceCollection AddHttpClientOptimized(this IServiceCollection services, IConfiguration config)
    {
        var perTryTimeoutSec = config.GetValue<int>("HttpClient:TimeoutSeconds", 10);

        services.AddHttpClient("default", client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan; 
                client.DefaultRequestHeaders.Add("User-Agent", "DigiTekShop-API/1.0");
                client.DefaultRequestVersion = new Version(2, 0);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
                                         | System.Net.DecompressionMethods.Deflate
                                         | System.Net.DecompressionMethods.Brotli,
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 20,
                EnableMultipleHttp2Connections = true
            })
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(perTryTimeoutSec)));

        return services;
    }


    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger("HttpClientPolly");
        var jitterer = new Random();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => (int)r.StatusCode == 429)
            .WaitAndRetryAsync(3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 250)),
                (outcome, delay, attempt, ctx) =>
                {
                    logger?.LogWarning("HTTP retry #{Attempt} after {Delay} due to {Reason}",
                        attempt, delay, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }


    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }


}

