using DigiTekShop.SharedKernel.DomainShared;

namespace DigiTekShop.Domain.Customers.Events;

public sealed class CustomerDefaultAddressChanged : DomainEvent
{
    public Guid CustomerId { get; }
    public int AddressIndex { get; }

    public CustomerDefaultAddressChanged(Guid customerId, int addressIndex)
    {
        CustomerId = customerId;
        AddressIndex = addressIndex;
    }
}