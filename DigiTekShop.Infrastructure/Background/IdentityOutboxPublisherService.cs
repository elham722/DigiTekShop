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

                var ids = await db.Set<OutboxMessage>()
                    .Where(x => x.Status == OutboxStatus.Pending)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Select(x => x.Id)
                    .Take(50)
                    .ToListAsync(ct);

                if (ids.Count == 0)
                {
                    await Task.Delay(500, ct);
                    continue;
                }

                foreach (var id in ids)
                {
                    if (!await OutboxSqlHelpers.TryClaimAsync(db, id, ct))
                        continue;

                    var msg = await db.Set<OutboxMessage>().FirstAsync(x => x.Id == id, ct);

                    try
                    {
                        await busRetry.ExecuteAsync(async () =>
                        {
                            await bus.PublishAsync(msg.Type, msg.Payload, ct);
                        });

                        await OutboxSqlHelpers.AckAsync(db, msg.Id, ct);
                    }
                    catch (Exception ex)
                    {
                        var attempts = msg.Attempts + 1;
                        var giveUp = attempts >= 10;
                        await OutboxSqlHelpers.NackAsync(db, msg.Id, attempts, giveUp, ex.Message, ct);

                        _log.LogError(ex, "Outbox publish failed (Shop). Id={Id}, Attempts={Attempts}, GiveUp={GiveUp}", msg.Id, attempts, giveUp);
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
