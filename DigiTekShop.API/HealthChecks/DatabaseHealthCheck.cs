using DigiTekShop.Identity.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DigiTekShop.API.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(DigiTekShopIdentityDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext hc, CancellationToken ct = default)
    {
        try
        {
            if (!await _context.Database.CanConnectAsync(ct))
            {
                _logger.LogWarning("DB health: CanConnectAsync returned false.");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

      
            await using var conn = _context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);

            var pending = await _context.Database.GetPendingMigrationsAsync(ct);

            var data = new Dictionary<string, object>
            {
                { "status", "Connected" },
                { "database", conn.Database },
                { "provider", _context.Database.ProviderName ?? "Unknown" },
                { "pendingMigrations", pending.Count() }
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


