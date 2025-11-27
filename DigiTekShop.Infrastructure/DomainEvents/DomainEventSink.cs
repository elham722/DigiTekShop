using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.DomainEvents;

public sealed class DomainEventSink : IDomainEventSink
{
    private readonly List<IDomainEvent> _events = new();
    private readonly ILogger<DomainEventSink>? _logger;

    public DomainEventSink(ILogger<DomainEventSink>? logger = null)
    {
        _logger = logger;
    }

    public void Raise(IDomainEvent @event)
    {
        if (@event is not null)
        {
            _events.Add(@event);
            _logger?.LogDebug("[DomainEventSink] Raised event: {EventType}, UserId={UserId}", 
                @event.GetType().Name, 
                @event is DigiTekShop.Identity.Events.UserRegisteredDomainEvent ur ? ur.UserId.ToString() : "N/A");
        }
    }

    public IReadOnlyCollection<IDomainEvent> PullAll()
    {
        var snapshot = _events.ToArray();
        _logger?.LogDebug("[DomainEventSink] PullAll: {Count} events", snapshot.Length);
        _events.Clear();
        return snapshot;
    }
}