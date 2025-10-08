using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace DigiTekShop.API.Extensions;

/// <summary>
/// Extensions for performance optimization
/// </summary>
public static class PerformanceExtensions
{
    /// <summary>
    /// Add response compression (Gzip, Brotli)
    /// </summary>
    public static IServiceCollection AddResponseCompressionOptimized(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            
            // MIME types to compress
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
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

        // Configure compression levels
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest; // or Optimal for better compression
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        return services;
    }

    /// <summary>
    /// Add output caching (ASP.NET Core 7+)
    /// </summary>
    public static IServiceCollection AddOutputCachingOptimized(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            // Default policy: cache for 60 seconds
            options.AddBasePolicy(builder => builder
                .Expire(TimeSpan.FromSeconds(60))
                .Tag("default"));

            // Policy for static content (cache for 1 hour)
            options.AddPolicy("StaticContent", builder => builder
                .Expire(TimeSpan.FromHours(1))
                .Tag("static")
                .SetVaryByHeader("Accept-Encoding"));

            // Policy for API responses (cache for 5 minutes)
            options.AddPolicy("ApiResponse", builder => builder
                .Expire(TimeSpan.FromMinutes(5))
                .Tag("api")
                .SetVaryByQuery("page", "pageSize", "sort")
                .SetVaryByHeader("Authorization"));
        });

        return services;
    }

    /// <summary>
    /// Configure HTTP client optimizations
    /// </summary>
    public static IServiceCollection AddHttpClientOptimized(this IServiceCollection services)
    {
        services.AddHttpClient("default", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "DigiTekShop-API/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 20,
            EnableMultipleHttp2Connections = true
        });

        return services;
    }
}

