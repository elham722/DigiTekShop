namespace DigiTekShop.Contracts.Integration.Events.Identity;

public sealed record UserUpdatedIntegrationEvent(
    Guid MessageId,
    Guid UserId,
    string? FullName,
    string? Email,
    string? PhoneNumber,
    bool? IsPhoneConfirmed,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null
);

