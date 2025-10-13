using DigiTekShop.Contracts.Abstractions.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigiTekShop.Infrastructure.Events;

public sealed class OutboxEventProcessor : BackgroundService, IOutboxEventProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);

    public OutboxEventProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxEventProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }
    }

    public async Task ProcessEventsAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var domainEventPublisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();

        var unprocessedEvents = await outboxRepository.GetUnprocessedEventsAsync(ct: ct);

        foreach (var outboxEvent in unprocessedEvents)
        {
            try
            {
                await ProcessEventAsync(outboxEvent, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox event {EventId}", outboxEvent.Id);
                await outboxRepository.MarkAsFailedAsync(outboxEvent.Id, ex.Message, ct);
            }
        }
    }

    public async Task ProcessEventAsync(OutboxEvent outboxEvent, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var domainEventPublisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();

        try
        {
            // Deserialize the domain event
            var eventType = Type.GetType(outboxEvent.EventType);
            if (eventType is null)
            {
                throw new InvalidOperationException($"Unknown event type: {outboxEvent.EventType}");
            }

            var domainEvent = JsonSerializer.Deserialize(outboxEvent.EventData, eventType);
            if (domainEvent is not IDomainEvent)
            {
                throw new InvalidOperationException("Deserialized object is not a domain event");
            }

            // Publish the domain event
            await domainEventPublisher.PublishAsync((IDomainEvent)domainEvent, ct);

            // Mark as processed
            await outboxRepository.MarkAsProcessedAsync(outboxEvent.Id, ct);

            _logger.LogInformation("Successfully processed outbox event {EventId} of type {EventType}",
                outboxEvent.Id, outboxEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process outbox event {EventId}", outboxEvent.Id);
            await outboxRepository.MarkAsFailedAsync(outboxEvent.Id, ex.Message, ct);
            throw;
        }
    }
}

