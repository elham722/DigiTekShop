using System.Text.Json;
using DigiTekShop.Application.Common.Events;
using DigiTekShop.Contracts.Abstractions.Telemetry; 
using DigiTekShop.Persistence.Context;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Enums.Outbox;
using DigiTekShop.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DigiTekShop.Persistence.Interceptors;

public sealed class ShopOutboxBeforeCommitInterceptor : SaveChangesInterceptor
{
    private readonly IIntegrationEventMapper _mapper;
    private readonly IDateTimeProvider _clock;
    private readonly ICorrelationContext? _corr; 

    public ShopOutboxBeforeCommitInterceptor(
        IIntegrationEventMapper mapper,
        IDateTimeProvider clock,
        ICorrelationContext? corr = null)
    {
        _mapper = mapper; _clock = clock; _corr = corr;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> r)
    { Process(e.Context); return r; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
    { Process(e.Context); return ValueTask.FromResult(r); }

    private void Process(DbContext? context)
    {
        if (context is not DigiTekShopDbContext ctx) return;

        Console.WriteLine("[ShopOutbox] Interceptor called for DigiTekShopDbContext");

        // از همین scope سرویس‌ها را بردار (اگر sink داری)
        var sink = ctx.GetService<IDomainEventSink>(); // ممکنه null باشه
        Console.WriteLine($"[ShopOutbox] DomainEventSink retrieved: {sink != null}");

        // 1) جمع کردن از Aggregateها
        var fromAggregates = ctx.ChangeTracker.Entries()
            .Where(x => x.Entity is IHasDomainEvents)
            .Select(x => (IHasDomainEvents)x.Entity)
            .SelectMany(a => a.PullDomainEvents())
            .ToList();

        // 2) جمع کردن از Sink (اگر استفاده می‌کنی)
        var fromSink = sink?.PullAll() ?? Array.Empty<IDomainEvent>();
        Console.WriteLine($"[ShopOutbox] Domain events from aggregates: {fromAggregates.Count}, from sink: {fromSink.Count()}");

        var domainEvents = fromAggregates.Concat(fromSink).ToList();
        if (domainEvents.Count == 0)
        {
            Console.WriteLine("[ShopOutbox] No domain events found, returning early");
            return;
        }

        // Map به IntegrationEvents
        var integrationEvents = _mapper.MapDomainEventsToIntegrationEvents(domainEvents).ToList();
        Console.WriteLine($"[ShopOutbox] Mapped to {integrationEvents.Count} integration events");

        // 👇 توجه: این باید مدل Outbox پرسیستنس خودت باشه
        var set = ctx.Set<DigiTekShop.Persistence.Models.OutboxMessage>(); // نام دقیق DbSet/Entity خودت

        var ambientCorrelation = _corr?.GetCorrelationId();
        var ambientCausation = _corr?.GetCausationId();

        foreach (var ie in integrationEvents)
        {
            var type = ie.GetType().FullName!;
            var payload = JsonSerializer.Serialize(ie);

            var corr = TryRead(ie, "CorrelationId") ?? ambientCorrelation;
            var caus = TryRead(ie, "CausationId") ?? ambientCausation ?? TryRead(ie, "MessageId");

            var msg = new DigiTekShop.Persistence.Models.OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = _clock.UtcNow,
                Type = type,
                Payload = payload,
                Status = OutboxStatus.Pending,
                Attempts = 0,
                CorrelationId = corr,
                CausationId = caus
            };

            set.Add(msg);
            Console.WriteLine($"[ShopOutbox] Adding message: {type} | Id: {msg.Id} | Corr={corr} | Caus={caus}");
        }
    }

    private static string? TryRead(object o, string p)
        => o.GetType().GetProperty(p)?.GetValue(o)?.ToString();
}
