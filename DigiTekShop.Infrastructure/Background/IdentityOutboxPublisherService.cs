using DigiTekShop.Application.Common.Messaging;
using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Enums.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Background;

public sealed class IdentityOutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<IdentityOutboxPublisherService> _log;

    public IdentityOutboxPublisherService(IServiceProvider sp, ILogger<IdentityOutboxPublisherService> log)
    { _sp = sp; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var busRetry = Policies.RetryBus();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DigiTekShopIdentityDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                var now = DateTimeOffset.UtcNow;
                var ids = await db.Set<IdentityOutboxMessage>()
                    .Where(x => x.Status == OutboxStatus.Pending
                                && (x.NextRetryUtc == null || x.NextRetryUtc <= now)
                                && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
                    .OrderBy(x => x.OccurredAtUtc)
                    .Select(x => x.Id)
                    .Take(50)
                    .ToListAsync(ct);

                if (ids.Count > 0)
                {
                    _log.LogInformation("[IdentityOutboxPublisher] Found {Count} pending messages", ids.Count);
                }

                if (ids.Count == 0)
                {
                    await Task.Delay(500, ct);
                    continue;
                }

                foreach (var id in ids)
                {
                    if (!await OutboxSqlHelpers.TryClaimAsync(db, id, ct))
                        continue;

                    var msg = await db.Set<IdentityOutboxMessage>().FirstAsync(x => x.Id == id, ct);

                    try
                    {
                        _log.LogInformation("[IdentityOutboxPublisher] Publishing message {Id}, Type={Type}", msg.Id, msg.Type);
                        await busRetry.ExecuteAsync(async () =>
                        {
                            await bus.PublishAsync(msg.Type, msg.Payload, ct);
                        });

                        _log.LogInformation("[IdentityOutboxPublisher] ✅ Successfully published message {Id}, Type={Type}", msg.Id, msg.Type);
                        // Use model method for success
                        msg.MarkAsSucceeded();
                        await db.SaveChangesAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        // Calculate exponential backoff: 1, 2, 4, 8, 16, ... minutes (max 60)
                        // Note: MarkAsFailed will increment Attempts, so we calculate based on current attempts
                        var nextAttempt = msg.Attempts + 1;
                        var giveUp = nextAttempt >= 10;
                        var delayMinutes = giveUp ? 0 : Math.Min(60, (int)Math.Pow(2, Math.Max(0, nextAttempt - 1)));
                        DateTimeOffset? nextRetryUtc = giveUp ? null : DateTimeOffset.UtcNow.AddMinutes(delayMinutes);

                        // Use model method for failure (model increments attempts itself)
                        msg.MarkAsFailed(ex.Message, nextRetryUtc);
                        await db.SaveChangesAsync(ct);

                        _log.LogError(ex, "Outbox publish failed. Id={Id}, Attempts={Attempts}, GiveUp={GiveUp}", msg.Id, msg.Attempts, giveUp);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "IdentityOutboxPublisherService loop error");
                await Task.Delay(1000, ct);
            }
        }
    }
}
