using DigiTekShop.Application.Common.Events;
using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.Domain.Customer.Events;

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
                    MessageId: Guid.NewGuid(),      // پیام یکتا برای Inbox
                    UserId: e.UserId,
                    Email: e.Email,
                    FullName: e.FullName,
                    OccurredOn: e.OccurredOn,
                    CorrelationId: e.CorrelationId
                );
            }

            // اگر رویدادهای دامنه‌ای دیگری هم داری، اینجا map کن...
        }
    }
}
