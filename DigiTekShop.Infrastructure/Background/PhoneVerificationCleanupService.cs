using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Background;

/// <summary>
/// Background service to periodically clean up expired and old verified PhoneVerification records
/// </summary>
public sealed class PhoneVerificationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PhoneVerificationCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(30); // Keep verified records for 30 days

    public PhoneVerificationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<PhoneVerificationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PhoneVerification cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredRecordsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during phone verification cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("PhoneVerification cleanup service stopped");
    }

    private async Task CleanupExpiredRecordsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DigiTekShopIdentityDbContext>();

        var now = DateTimeOffset.UtcNow;
        var cutoffDate = now.Subtract(_retentionPeriod);

        // Delete expired and old verified records
        // Note: ExecuteDeleteAsync requires SQL-translatable expressions (no domain methods)
        var deletedCount = await db.PhoneVerifications
            .Where(pv =>
                (!pv.IsVerified && pv.ExpiresAtUtc <= now) || // Expired and not verified
                (pv.IsVerified && pv.VerifiedAtUtc.HasValue && pv.VerifiedAtUtc < cutoffDate)) // Old verified records
            .ExecuteDeleteAsync(ct);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} phone verification records", deletedCount);
        }
    }
}

