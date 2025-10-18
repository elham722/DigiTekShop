using DigiTekShop.Application.Common.Events;

namespace DigiTekShop.Application.Outbox
{
    public sealed class IdentityIntegrationEventMapper : IIntegrationEventMapper
    {
        public IEnumerable<object> MapDomainEventsToIntegrationEvents(IEnumerable<object> domainEvents)
        {
            // TODO: map رویدادهای هویتی → IntegrationEvent
            return Enumerable.Empty<object>();
        }
    }
}
