namespace DigiTekShop.Domain.Customer.Events;

public sealed record CustomerEmailChanged(
    Guid CustomerId,
    string OldEmail,
    string NewEmail
) : DomainEvent;