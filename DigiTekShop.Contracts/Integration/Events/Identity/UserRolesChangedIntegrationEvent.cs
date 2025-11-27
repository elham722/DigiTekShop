namespace DigiTekShop.Contracts.Integration.Events.Identity;

public sealed record UserRolesChangedIntegrationEvent(
    Guid MessageId,
    Guid UserId,
    IReadOnlyList<string> Roles,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null
);

