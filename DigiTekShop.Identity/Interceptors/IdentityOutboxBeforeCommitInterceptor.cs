using DigiTekShop.Application.Common.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;       // IHasDomainEvents + IDomainEventSink
using DigiTekShop.SharedKernel.Enums.Outbox;
using DigiTekShop.SharedKernel.Time;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace DigiTekShop.Identity.Interceptors;

public sealed class IdentityOutboxBeforeCommitInterceptor : SaveChangesInterceptor
{
    private readonly IIntegrationEventMapper _mapper;
    private readonly IDateTimeProvider _clock;
    private readonly IDomainEventSink _sink;

    public IdentityOutboxBeforeCommitInterceptor(
        IIntegrationEventMapper mapper,
        IDateTimeProvider clock,
        IDomainEventSink sink)
    {
        _mapper = mapper; _clock = clock; _sink = sink;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not DigiTekShopIdentityDbContext ctx) return result;

        // 1) از Aggregateها (اگر داشته باشیم)
        var fromAggregates = ctx.ChangeTracker.Entries()
            .Where(e => e.Entity is IHasDomainEvents)
            .Select(e => (IHasDomainEvents)e.Entity)
            .SelectMany(a => a.PullDomainEvents());

        // 2) از Sink (برای سناریوهایی مثل UserManager)
        var fromSink = _sink.PullAll();

        var domainEvents = fromAggregates.Concat(fromSink).ToList();
        if (domainEvents.Count == 0) return result;

        var integrationEvents = _mapper.MapDomainEventsToIntegrationEvents(domainEvents);

        var set = ctx.Set<OutboxMessage>();
        foreach (var ie in integrationEvents)
        {
            set.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = _clock.UtcNow,
                Type = ie.GetType().FullName!,
                Payload = JsonSerializer.Serialize(ie),
                Status = OutboxStatus.Pending,
                Attempts = 0
            });
        }

        return result;
    }
}
