using DigiTekShop.Application.Common.Events;

namespace DigiTekShop.Application.Outbox;

public sealed class ShopIntegrationEventMapper : IIntegrationEventMapper
{
    public IEnumerable<object> MapDomainEventsToIntegrationEvents(IEnumerable<object> domainEvents)
    {
        // TODO: اینجا بر اساس نوع DomainEvent ها، IntegrationEvent بساز
        // مثال: if (de is CustomerRegistered e) yield return new CustomerRegisteredIntegrationEvent(...);
        return Enumerable.Empty<object>();
    }
}