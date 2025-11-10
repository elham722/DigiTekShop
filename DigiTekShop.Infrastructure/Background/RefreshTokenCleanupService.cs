using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Background;

/// <summary>
/// Background service to periodically clean up expired and old revoked RefreshToken records
/// </summary>
public sealed class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(90); // Keep revoked/expired records for 90 days for audit trail

    public RefreshTokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefreshToken cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredRecordsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during refresh token cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("RefreshToken cleanup service stopped");
    }

    private async Task CleanupExpiredRecordsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DigiTekShopIdentityDbContext>();

        var now = DateTimeOffset.UtcNow;
        var cutoffDate = now.Subtract(_retentionPeriod);

        // Delete expired and old revoked records
        // Note: ExecuteDeleteAsync requires SQL-translatable expressions (no domain methods)
        var deletedCount = await db.RefreshTokens
            .IgnoreQueryFilters() // Include soft-deleted users' tokens for cleanup
            .Where(rt =>
                (rt.ExpiresAtUtc <= now && rt.RevokedAtUtc == null) || // Expired and not revoked
                (rt.RevokedAtUtc.HasValue && rt.RevokedAtUtc < cutoffDate)) // Old revoked records
            .ExecuteDeleteAsync(ct);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} refresh token records", deletedCount);
        }
    }
}

