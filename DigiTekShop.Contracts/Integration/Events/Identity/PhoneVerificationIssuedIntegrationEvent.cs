namespace DigiTekShop.Contracts.Integration.Events.Identity;
public sealed record PhoneVerificationIssuedIntegrationEvent(
    Guid MessageId,
    Guid UserId,
    string PhoneNumber,
    Guid PhoneVerificationId,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null,
    string? CausationId = null
);

