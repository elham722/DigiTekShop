namespace DigiTekShop.Domain.Customer.Events;

public sealed record CustomerDefaultAddressChanged(
    Guid CustomerId,
    int AddressIndex
) : DomainEvent;