using MediatR;
using DigiTekShop.Contracts.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Infrastructure.Events;

public sealed class MediatRDomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher; // MediatR publisher
    public MediatRDomainEventPublisher(IPublisher publisher) => _publisher = publisher;

    public async Task PublishAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(@event.GetType());
        var notification = Activator.CreateInstance(notificationType, @event) as INotification;
        if (notification is not null)
            await _publisher.Publish(notification, ct);
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var e in events)
            await PublishAsync(e, ct);
    }
}