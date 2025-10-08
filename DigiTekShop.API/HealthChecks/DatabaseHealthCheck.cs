using DigiTekShop.Identity.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DigiTekShop.API.HealthChecks;

/// <summary>
/// Custom health check for Database connectivity
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(DigiTekShopIdentityDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connectivity with a simple query
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                _logger.LogWarning("Database health check failed: Unable to connect");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Get database info
            var connectionString = _context.Database.GetConnectionString();
            var databaseName = _context.Database.GetDbConnection().Database;
            
            // Test a simple query
            var userCount = await _context.Users.CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "status", "Connected" },
                { "database", databaseName },
                { "provider", _context.Database.ProviderName ?? "Unknown" },
                { "total_users", userCount }
            };

            return HealthCheckResult.Healthy("Database is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}

