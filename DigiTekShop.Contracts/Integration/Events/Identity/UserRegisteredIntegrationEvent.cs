namespace DigiTekShop.Contracts.Integration.Events.Identity;

public sealed record UserRegisteredIntegrationEvent(
    Guid MessageId,          
    Guid UserId,
    string Email,
    string? FullName,
    string? PhoneNumber,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null
);