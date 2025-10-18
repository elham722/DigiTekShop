using System.Text.Json;
using DigiTekShop.Application.Common.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Enums.Outbox;
using DigiTekShop.SharedKernel.Time;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DigiTekShop.Identity.Interceptors
{

    public sealed class IdentityOutboxBeforeCommitInterceptor : SaveChangesInterceptor
    {
        private readonly IIntegrationEventMapper _mapper;
        private readonly IDateTimeProvider _clock;

        public IdentityOutboxBeforeCommitInterceptor(IIntegrationEventMapper mapper, IDateTimeProvider clock)
        {
            _mapper = mapper; _clock = clock;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> r)
        {
            if (e.Context is not DigiTekShopIdentityDbContext ctx) return r;

            // ✅ همینجا Sink را از اسکوپِ همان DbContext بگیر
            var sink = ctx.GetService<IDomainEventSink>();

            var fromAggregates = ctx.ChangeTracker.Entries()
                .Where(x => x.Entity is IHasDomainEvents)
                .Select(x => (IHasDomainEvents)x.Entity)
                .SelectMany(a => a.PullDomainEvents());

            var fromSink = sink.PullAll();

            var domainEvents = fromAggregates.Concat(fromSink).ToList();
            if (domainEvents.Count == 0) return r;

            var integrationEvents = _mapper.MapDomainEventsToIntegrationEvents(domainEvents);

            var set = ctx.Set<DigiTekShop.Identity.Models.OutboxMessage>(); // مطمئن و بدون ابهام
            foreach (var ie in integrationEvents)
            {
                set.Add(new DigiTekShop.Identity.Models.OutboxMessage
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