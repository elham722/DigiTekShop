using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Domain.Customer.Events;

public sealed class CustomerRegistered : DomainEvent
{
    public Guid CustomerId { get; }
    public Guid UserId { get; }
    public string Email { get; }

    public CustomerRegistered(Guid customerId, Guid userId, string email)
    {
        CustomerId = customerId;
        UserId = userId;
        Email = email;
    }
}