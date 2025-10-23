namespace DigiTekShop.Identity.Events;

public sealed class IdentityIntegrationEventMapper : IIntegrationEventMapper
{
    public IEnumerable<object> MapDomainEventsToIntegrationEvents(IEnumerable<object> domainEvents)
    {
        foreach (var de in domainEvents)
        {
            if (de is UserRegisteredDomainEvent e)
            {
                yield return new UserRegisteredIntegrationEvent(
                    MessageId: Guid.NewGuid(),
                    UserId: e.UserId,
                    Email: e.Email,
                    FullName: e.FullName,
                    PhoneNumber: e.PhoneNumber,
                    OccurredOn: e.OccurredOn,
                    CorrelationId: e.CorrelationId
                );
            }
        }
    }
}