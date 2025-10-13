using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace DigiTekShop.API.Extensions;

public static class PerformanceExtensions
{
   
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

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest; 
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
                    var path = ctx.HttpContext.Request.Path.Value ?? string.Empty;
                    return !System.Text.RegularExpressions.Regex.IsMatch(
                        path, @"^/api/v\d+/(auth|registration|password|twofactor)/",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
                .SetVaryByQuery("page", "pageSize", "sort")
                .SetVaryByHeader("Authorization"));
        });
        return services;
    }

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

