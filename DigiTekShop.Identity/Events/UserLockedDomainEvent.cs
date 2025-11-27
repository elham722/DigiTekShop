namespace DigiTekShop.Identity.Events;

public sealed record UserLockedDomainEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public DateTimeOffset LockoutEnd { get; init; }

    public UserLockedDomainEvent(
        Guid userId,
        DateTimeOffset lockoutEnd,
        DateTimeOffset? occurredOn = null,
        string? correlationId = null)
        : base(occurredOn, correlationId)
    {
        UserId = userId;
        LockoutEnd = lockoutEnd;
    }
}

