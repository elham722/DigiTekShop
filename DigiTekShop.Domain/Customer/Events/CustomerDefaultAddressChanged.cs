using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Domain.Customer.Events;

public sealed class CustomerDefaultAddressChanged : DomainEvent
{
    public Guid CustomerId { get; }
    public int AddressIndex { get; }

    public CustomerDefaultAddressChanged(Guid customerId, int addressIndex):base()
    {
        CustomerId = customerId;
        AddressIndex = addressIndex;
    }
}