namespace DigiTekShop.Identity.Events;

public sealed record UserUnlockedDomainEvent : DomainEvent
{
    public Guid UserId { get; init; }

    public UserUnlockedDomainEvent(
        Guid userId,
        DateTimeOffset? occurredOn = null,
        string? correlationId = null)
        : base(occurredOn, correlationId)
    {
        UserId = userId;
    }
}

