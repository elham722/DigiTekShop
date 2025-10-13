using DigiTekShop.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using DigiTekShop.API.Common.Http;
namespace DigiTekShop.API.Extensions.HealthCheck;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddComprehensiveHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database", "infrastructure" })
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

                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

                var cid = context.Items.TryGetValue(HeaderNames.CorrelationId, out var v) ? v?.ToString() : context.TraceIdentifier;

                var result = JsonSerializer.Serialize(new
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
                }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                await context.Response.WriteAsync(result);
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
