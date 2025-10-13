using DigiTekShop.Contracts.Abstractions.Events;
using DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DigiTekShop.Contracts.Abstractions.Events.Outbox;
using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Infrastructure.Events
{

    public sealed class AppOutboxBridge : IAppOutboxBridge
    {
        private readonly IUnitOfWork _uow; // از AppDbContext
        private readonly IOutboxEventRepository _outbox;

        public AppOutboxBridge(IUnitOfWork uow, IOutboxEventRepository outbox)
            => (_uow, _outbox) = (uow, outbox);

        public async Task EnqueueAsync<TEvent>(TEvent evt, CancellationToken ct = default)
        {
            // تراکنش محلی App
            await _uow.ExecuteInTransactionAsync(async _ =>
            {
                await _outbox.AddAsync(new OutboxEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = typeof(TEvent).AssemblyQualifiedName!,
                    EventData = JsonSerializer.Serialize(evt),
                    AggregateId = Guid.NewGuid().ToString(), // اگر لازم داری چیزی خاص بگذار
                    AggregateType = typeof(TEvent).Name,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }, ct);
        }
    }

}
