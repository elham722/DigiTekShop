namespace DigiTekShop.Contracts.Integration.Events.Identity;

public sealed record UserUnlockedIntegrationEvent(
    Guid MessageId,
    Guid UserId,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null
);

