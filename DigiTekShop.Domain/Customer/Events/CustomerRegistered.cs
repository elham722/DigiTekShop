using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Domain.Customer.Events;

public sealed record CustomerRegistered(
    Guid CustomerId,
    Guid UserId,
    DateTimeOffset OccurredOn,
    string? CorrelationId = null
) : DomainEvent;