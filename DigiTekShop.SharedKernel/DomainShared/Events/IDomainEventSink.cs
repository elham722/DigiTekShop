namespace DigiTekShop.SharedKernel.DomainShared.Events;

public interface IDomainEventSink
{
    void Raise(IDomainEvent @event);
    IReadOnlyCollection<IDomainEvent> PullAll();
}