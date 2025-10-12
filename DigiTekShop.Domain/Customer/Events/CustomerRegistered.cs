namespace DigiTekShop.Domain.Customer.Events;

public sealed record CustomerRegistered(
    Guid CustomerId,
    Guid UserId,
    string Email
) : DomainEvent;
