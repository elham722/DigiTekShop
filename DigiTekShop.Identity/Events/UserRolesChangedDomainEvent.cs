namespace DigiTekShop.Identity.Events;

public sealed record UserRolesChangedDomainEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public UserRolesChangedDomainEvent(
        Guid userId,
        IReadOnlyList<string> roles,
        DateTimeOffset? occurredOn = null,
        string? correlationId = null)
        : base(occurredOn, correlationId)
    {
        UserId = userId;
        Roles = roles;
    }
}

