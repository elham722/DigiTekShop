using DigiTekShop.Application.Common.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Enums.Outbox;
using DigiTekShop.SharedKernel.Time;
using System.Text.Json;

namespace DigiTekShop.Identity.Events;

public sealed class OutboxFlusher
{
    private readonly IIntegrationEventMapper _mapper;
    private readonly IDateTimeProvider _clock;

    public OutboxFlusher(IIntegrationEventMapper mapper, IDateTimeProvider clock)
    {
        _mapper = mapper;
        _clock = clock;
    }

    public void Flush(IDomainEventSink sink, DigiTekShopIdentityDbContext ctx)
    {
        var domainEvents = sink.PullAll();
        if (domainEvents.Count == 0) return;

        var integration = _mapper.MapDomainEventsToIntegrationEvents(domainEvents);
        var set = ctx.Set<OutboxMessage>();

        foreach (var ie in integration)
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
    }
}