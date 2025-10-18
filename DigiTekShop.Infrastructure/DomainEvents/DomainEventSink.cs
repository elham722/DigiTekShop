using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Infrastructure.DomainEvents;

public sealed class DomainEventSink : IDomainEventSink
{
    private readonly List<IDomainEvent> _events = new();

    public void Raise(IDomainEvent @event)
    {
        if (@event is not null)
            _events.Add(@event);
    }

    public IReadOnlyCollection<IDomainEvent> PullAll()
    {
        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }
}