using System.Text.Json;
using DigiTekShop.Application.Common.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Enums.Outbox;
using DigiTekShop.SharedKernel.Time;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigiTekShop.Identity.Interceptors
{
    public sealed class IdentityOutboxBeforeCommitInterceptor : SaveChangesInterceptor
    {
        private readonly IIntegrationEventMapper _mapper;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventSink _sink;


        public IdentityOutboxBeforeCommitInterceptor(
            IIntegrationEventMapper mapper,
            IDateTimeProvider clock,
            IDomainEventSink sink)
        { _mapper = mapper; _clock = clock; _sink = sink; }

        public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> r)
        {
            if (e.Context is not DigiTekShopIdentityDbContext ctx) return r;

            var fromAggregates = ctx.ChangeTracker.Entries()
                .Where(x => x.Entity is IHasDomainEvents)
                .Select(x => (IHasDomainEvents)x.Entity)
                .SelectMany(a => a.PullDomainEvents());

            var fromSink = _sink.PullAll();

            var domainEvents = fromAggregates.Concat(fromSink).ToList();
            if (domainEvents.Count == 0) return r;

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
            return r;
        }
    }
}