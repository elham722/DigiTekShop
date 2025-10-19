using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Domain.Customer.Events;

public sealed record CustomerRegistered : DomainEvent
{
    public Guid CustomerId { get; init; }
    public Guid UserId { get; init; }

    public CustomerRegistered(
        Guid CustomerId,
        Guid UserId,
        DateTimeOffset OccurredOn,
        string? CorrelationId = null)
        : base(OccurredOn, CorrelationId)
    {
        this.CustomerId = CustomerId;
        this.UserId = UserId;
    }
}