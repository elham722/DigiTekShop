namespace DigiTekShop.Identity.Events.PhoneVerification;
public sealed record PhoneVerificationIssuedDomainEvent(
    Guid UserId,
    string PhoneNumber,
    Guid PhoneVerificationId,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null
) : DomainEvent(OccurredOn, CorrelationId);

