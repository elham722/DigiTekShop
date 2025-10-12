using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Contracts.Abstractions.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent @event, CancellationToken ct = default);
    Task PublishAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}