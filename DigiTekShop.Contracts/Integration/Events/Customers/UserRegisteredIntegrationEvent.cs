namespace DigiTekShop.Contracts.Integration.Events.Customers;

public sealed record UserRegisteredIntegrationEvent(
    Guid MessageId,          
    Guid UserId,
    string Email,
    string? FullName,
    string? PhoneNumber,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null
);