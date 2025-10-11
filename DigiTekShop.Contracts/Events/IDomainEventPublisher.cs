using DigiTekShop.SharedKernel.DomainShared;

namespace DigiTekShop.Contracts.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent @event, CancellationToken ct = default);
    Task PublishAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}