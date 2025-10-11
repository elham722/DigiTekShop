using DigiTekShop.SharedKernel.DomainShared;

namespace DigiTekShop.Domain.Customers.Events;

public sealed class CustomerEmailChanged : DomainEvent
{
    public Guid CustomerId { get; }
    public string OldEmail { get; }
    public string NewEmail { get; }

    public CustomerEmailChanged(Guid customerId, string oldEmail, string newEmail)
    {
        CustomerId = customerId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
    }
}