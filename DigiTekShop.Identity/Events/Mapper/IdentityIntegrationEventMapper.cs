using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.Identity.Events.PhoneVerification;

namespace DigiTekShop.Identity.Events.Mapper;

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
            if (de is PhoneVerificationIssuedDomainEvent eDe)
            {
                yield return new PhoneVerificationIssuedIntegrationEvent(
                    MessageId: Guid.NewGuid(),
                    UserId: eDe.UserId,
                    PhoneNumber: eDe.PhoneNumber,
                    PhoneVerificationId: eDe.PhoneVerificationId,
                    OccurredOn: eDe.OccurredOn,
                    CorrelationId: eDe.CorrelationId
                );
            }
            if (de is UserUpdatedDomainEvent userUpdated)
            {
                yield return new UserUpdatedIntegrationEvent(
                    MessageId: Guid.NewGuid(),
                    UserId: userUpdated.UserId,
                    FullName: userUpdated.FullName,
                    Email: userUpdated.Email,
                    PhoneNumber: userUpdated.PhoneNumber,
                    IsPhoneConfirmed: userUpdated.IsPhoneConfirmed,
                    OccurredOn: userUpdated.OccurredOn,
                    CorrelationId: userUpdated.CorrelationId
                );
            }
            if (de is UserLockedDomainEvent userLocked)
            {
                yield return new UserLockedIntegrationEvent(
                    MessageId: Guid.NewGuid(),
                    UserId: userLocked.UserId,
                    LockoutEnd: userLocked.LockoutEnd,
                    OccurredOn: userLocked.OccurredOn,
                    CorrelationId: userLocked.CorrelationId
                );
            }
            if (de is UserUnlockedDomainEvent userUnlocked)
            {
                yield return new UserUnlockedIntegrationEvent(
                    MessageId: Guid.NewGuid(),
                    UserId: userUnlocked.UserId,
                    OccurredOn: userUnlocked.OccurredOn,
                    CorrelationId: userUnlocked.CorrelationId
                );
            }
            if (de is UserRolesChangedDomainEvent rolesChanged)
            {
                yield return new UserRolesChangedIntegrationEvent(
                    MessageId: Guid.NewGuid(),
                    UserId: rolesChanged.UserId,
                    Roles: rolesChanged.Roles,
                    OccurredOn: rolesChanged.OccurredOn,
                    CorrelationId: rolesChanged.CorrelationId
                );
            }
        }
    }
}