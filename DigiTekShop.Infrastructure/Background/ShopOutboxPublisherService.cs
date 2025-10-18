using DigiTekShop.Application.Common.Messaging;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Outbox;
using DigiTekShop.SharedKernel.Enums.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigiTekShop.Infrastructure.Background;

public sealed class ShopOutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ShopOutboxPublisherService> _log;

    public ShopOutboxPublisherService(IServiceProvider sp, ILogger<ShopOutboxPublisherService> log)
    { _sp = sp; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var busRetry = Policies.RetryBus();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DigiTekShopDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                // فقط آیدی‌ها را بگیر تا سبک باشد
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
                    // CLAIM: اگر نتوانستیم claim کنیم یعنی کسی دیگر برداشت
                    if (!await OutboxSqlHelpers.TryClaimAsync(db, id, ct))
                        continue;

                    // حالا رکورد را برای payload بخوان
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
                _log.LogError(ex, "ShopOutboxPublisherService loop error");
                await Task.Delay(1000, ct);
            }
        }
    }
}
