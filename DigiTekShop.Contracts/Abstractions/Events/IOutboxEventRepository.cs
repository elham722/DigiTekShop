using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Contracts.Abstractions.Events;

public interface IOutboxEventRepository
{
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken ct = default);

    Task<IEnumerable<OutboxEvent>> GetUnprocessedEventsAsync(int batchSize = 100, CancellationToken ct = default);

    Task MarkAsProcessedAsync(Guid eventId, CancellationToken ct = default);

    Task MarkAsFailedAsync(Guid eventId, string errorMessage, CancellationToken ct = default);

    Task DeleteProcessedEventsAsync(DateTime olderThan, CancellationToken ct = default);
}
