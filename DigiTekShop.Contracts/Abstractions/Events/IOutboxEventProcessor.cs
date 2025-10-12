using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Contracts.Abstractions.Events;

public interface IOutboxEventProcessor
{
    Task ProcessEventsAsync(CancellationToken ct = default);

    Task ProcessEventAsync(OutboxEvent outboxEvent, CancellationToken ct = default);
}
