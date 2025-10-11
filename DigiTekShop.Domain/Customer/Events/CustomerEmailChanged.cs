using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Domain.Customer.Events;

public sealed class CustomerEmailChanged : DomainEvent
{
    public Guid CustomerId { get; }
    public string OldEmail { get; }
    public string NewEmail { get; }

    public CustomerEmailChanged(Guid customerId, string oldEmail, string newEmail):base()
    {
        CustomerId = customerId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
    }
}