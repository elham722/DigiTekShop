using System.Text.Json;
using DigiTekShop.API.Common.Http;
using DigiTekShop.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DigiTekShop.API.Extensions.HealthCheck;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddComprehensiveHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database", "infrastructure" },
                timeout: TimeSpan.FromSeconds(3))
            .AddCheck<RedisHealthCheck>(
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "redis", "infrastructure" },
                timeout: TimeSpan.FromSeconds(2))
            .AddCheck<RabbitMQHealthCheck>(
                name: "rabbitmq",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "rabbitmq", "infrastructure" },
                timeout: TimeSpan.FromSeconds(3))
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "api" });

        return services;
    }

    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";

                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                var cid = context.Items.TryGetValue(HeaderNames.CorrelationId, out var v)
                          ? v?.ToString()
                          : context.TraceIdentifier;

                var payload = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    correlationId = cid,
                    duration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds,
                        exception = env.IsDevelopment() ? e.Value.Exception?.Message : null,
                        data = e.Value.Data?.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString())
                    })
                };

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
            }
        });

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("api"),
            AllowCachingResponses = false
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("infrastructure"),
            AllowCachingResponses = false
        });

        return endpoints;
    }
}
