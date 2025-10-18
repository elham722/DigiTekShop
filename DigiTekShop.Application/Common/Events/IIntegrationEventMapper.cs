namespace DigiTekShop.Application.Common.Events;

public interface IIntegrationEventMapper
{
    IEnumerable<object> MapDomainEventsToIntegrationEvents(IEnumerable<object> domainEvents);
}