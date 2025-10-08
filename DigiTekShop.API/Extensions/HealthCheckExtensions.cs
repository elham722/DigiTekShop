using DigiTekShop.API.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace DigiTekShop.API.Extensions;

/// <summary>
/// Extensions for configuring health checks
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Add comprehensive health checks for the application
    /// </summary>
    public static IServiceCollection AddComprehensiveHealthChecks(this IServiceCollection services)
    {
        // Note: Redis health check is already registered in Infrastructure layer
        // We only add custom checks here to avoid duplicate registrations
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "database", "infrastructure" })
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "api" });

        return services;
    }

    /// <summary>
    /// Map health check endpoints with custom response formatting
    /// </summary>
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Detailed health endpoint (JSON)
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    duration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds,
                        exception = e.Value.Exception?.Message,
                        data = e.Value.Data
                    })
                }, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(result);
            }
        });

        // Simple liveness endpoint (for load balancers)
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("api"),
            AllowCachingResponses = false
        });

        // Readiness endpoint (for Kubernetes)
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("infrastructure"),
            AllowCachingResponses = false
        });

        return endpoints;
    }
}

