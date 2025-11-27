namespace DigiTekShop.Contracts.Integration.Events.Identity;

public sealed record UserLockedIntegrationEvent(
    Guid MessageId,
    Guid UserId,
    DateTimeOffset LockoutEnd,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null
);

