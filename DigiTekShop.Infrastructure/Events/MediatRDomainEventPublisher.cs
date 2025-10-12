using MediatR;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.Contracts.Abstractions.Events;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Events;


public sealed class MediatRDomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<MediatRDomainEventPublisher> _logger;

    public MediatRDomainEventPublisher(IPublisher publisher, ILogger<MediatRDomainEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        var eventType = @event.GetType();
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
        var notification = Activator.CreateInstance(notificationType, @event) as INotification;
        
        if (notification is null)
        {
            _logger.LogWarning("Failed to create notification for event type: {EventType}", eventType.Name);
            return;
        }

        _logger.LogDebug("Publishing domain event: {EventType}", eventType.Name);
        await _publisher.Publish(notification, ct);
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        var eventList = events.ToList();
        
        if (eventList.Count == 0)
            return;

        _logger.LogDebug("Publishing {Count} domain events", eventList.Count);

        foreach (var @event in eventList)
        {
            await PublishAsync(@event, ct);
        }
    }
}