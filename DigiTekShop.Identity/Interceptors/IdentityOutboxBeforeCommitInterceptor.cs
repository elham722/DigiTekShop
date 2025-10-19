using System.Text.Json;
using DigiTekShop.Application.Common.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Enums.Outbox;
using DigiTekShop.SharedKernel.Time;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DigiTekShop.Identity.Interceptors
{

    public sealed class IdentityOutboxBeforeCommitInterceptor : SaveChangesInterceptor
    {
        private readonly IIntegrationEventMapper _mapper;
        private readonly IDateTimeProvider _clock;
        private readonly IServiceProvider _serviceProvider;

        public IdentityOutboxBeforeCommitInterceptor(IIntegrationEventMapper mapper, IDateTimeProvider clock, IServiceProvider serviceProvider)
        {
            _mapper = mapper; 
            _clock = clock;
            _serviceProvider = serviceProvider;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData e, InterceptionResult<int> r)
        {
            ProcessDomainEvents(e.Context);
            return r;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData e, 
            InterceptionResult<int> r, 
            CancellationToken ct = default)
        {
            ProcessDomainEvents(e.Context);
            return ValueTask.FromResult(r);
        }

        private void ProcessDomainEvents(DbContext? context)
        {
            if (context is not DigiTekShopIdentityDbContext ctx) return;

            Console.WriteLine("[IdentityOutbox] Interceptor called for DigiTekShopIdentityDbContext");

            // ✅ استفاده از ctx.GetService به جای scope جدید
            // چون باید از همان scope استفاده کنیم که RegistrationService استفاده می‌کند
            var sink = ctx.GetService<IDomainEventSink>();
            Console.WriteLine($"[IdentityOutbox] DomainEventSink retrieved: {sink != null}");

            var fromAggregates = ctx.ChangeTracker.Entries()
                .Where(x => x.Entity is IHasDomainEvents)
                .Select(x => (IHasDomainEvents)x.Entity)
                .SelectMany(a => a.PullDomainEvents());

            var fromSink = sink?.PullAll() ?? Array.Empty<IDomainEvent>();
            Console.WriteLine($"[IdentityOutbox] Domain events from aggregates: {fromAggregates.Count()}, from sink: {fromSink.Count}");

            var domainEvents = fromAggregates.Concat(fromSink).ToList();
            if (domainEvents.Count == 0) 
            {
                Console.WriteLine("[IdentityOutbox] No domain events found, returning early");
                return;
            }

            var integrationEvents = _mapper.MapDomainEventsToIntegrationEvents(domainEvents).ToList();
            Console.WriteLine($"[IdentityOutbox] Mapped to {integrationEvents.Count} integration events");
            
            var set = ctx.Set<DigiTekShop.Identity.Models.IdentityOutboxMessage>();

            foreach (var ie in integrationEvents)
            {
                var outboxMsg = new DigiTekShop.Identity.Models.IdentityOutboxMessage()
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = _clock.UtcNow,
                    Type = ie.GetType().FullName!,
                    Payload = JsonSerializer.Serialize(ie),
                    Status = OutboxStatus.Pending,
                    Attempts = 0
                };
                set.Add(outboxMsg);
                
                // ✅ Debug log
                Console.WriteLine($"[IdentityOutbox] Adding message: {outboxMsg.Type} | Id: {outboxMsg.Id}");
            }
        }

    }

}