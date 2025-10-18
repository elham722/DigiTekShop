using DigiTekShop.Application.Common.Events;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Outbox;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Enums.Outbox;
using DigiTekShop.SharedKernel.Time;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;


namespace DigiTekShop.Persistence.Interceptors;

public sealed class ShopOutboxBeforeCommitInterceptor : SaveChangesInterceptor
{
    private readonly IIntegrationEventMapper _mapper;
    private readonly IDateTimeProvider _clock;

    public ShopOutboxBeforeCommitInterceptor(IIntegrationEventMapper mapper, IDateTimeProvider clock)
    {
        _mapper = mapper;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not DigiTekShopDbContext ctx) return result;

        var aggregates = ctx.ChangeTracker.Entries()
            .Where(e => e.Entity is IHasDomainEvents)
            .Select(e => (IHasDomainEvents)e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.PullDomainEvents())  // حتماً این متد در AggregateRoot<TId> پیاده شده باشد
            .ToList();

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