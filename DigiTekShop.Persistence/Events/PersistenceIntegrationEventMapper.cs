using DigiTekShop.Application.Common.Events;
using DigiTekShop.Contracts.Integration.Events.Customers;
using DigiTekShop.Domain.Customer.Events;

namespace DigiTekShop.Persistence.Events;

public sealed class PersistenceIntegrationEventMapper : IIntegrationEventMapper
{
    public IEnumerable<object> MapDomainEventsToIntegrationEvents(IEnumerable<object> domainEvents)
    {
        foreach (var de in domainEvents)
        {
            if (de is CustomerRegistered e)
            {
                yield return new AddCustomerIdIntegrationEvent(
                    MessageId: Guid.NewGuid(),
                    UserId: e.UserId,
                    CustomerId: e.CustomerId,
                    OccurredOn: DateTimeOffset.UtcNow,
                    CorrelationId: e.CorrelationId,
                    CausationId: null
                );
            }
        }
    }
}
