using DigiTekShop.Contracts.Abstractions.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Infrastructure.Events;


public sealed class InfrastructureOutboxEventRepository : IOutboxEventRepository
{
    private static readonly List<OutboxEvent> _events = new();
    private static readonly object _lock = new();

    public Task AddAsync(OutboxEvent outboxEvent, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _events.Add(outboxEvent);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<OutboxEvent>> GetUnprocessedEventsAsync(int batchSize = 100, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var unprocessed = _events
                .Where(e => !e.IsProcessed && e.RetryCount < 3)
                .OrderBy(e => e.CreatedAt)
                .Take(batchSize)
                .ToList();
            
            return Task.FromResult<IEnumerable<OutboxEvent>>(unprocessed);
        }
    }

    public Task MarkAsProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var outboxEvent = _events.FirstOrDefault(e => e.Id == eventId);
            if (outboxEvent is not null)
            {
                outboxEvent.ProcessedAt = DateTime.UtcNow;
                outboxEvent.ErrorMessage = null;
            }
        }
        return Task.CompletedTask;
    }

    public Task MarkAsFailedAsync(Guid eventId, string errorMessage, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var outboxEvent = _events.FirstOrDefault(e => e.Id == eventId);
            if (outboxEvent is not null)
            {
                outboxEvent.RetryCount++;
                outboxEvent.ErrorMessage = errorMessage;
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteProcessedEventsAsync(DateTime olderThan, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _events.RemoveAll(e => e.IsProcessed && e.ProcessedAt < olderThan);
        }
        return Task.CompletedTask;
    }
}
