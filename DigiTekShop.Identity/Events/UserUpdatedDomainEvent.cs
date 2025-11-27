namespace DigiTekShop.Identity.Events;

public sealed record UserUpdatedDomainEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? IsPhoneConfirmed { get; init; }

    public UserUpdatedDomainEvent(
        Guid userId,
        string? fullName = null,
        string? email = null,
        string? phoneNumber = null,
        bool? isPhoneConfirmed = null,
        DateTimeOffset? occurredOn = null,
        string? correlationId = null)
        : base(occurredOn, correlationId)
    {
        UserId = userId;
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        IsPhoneConfirmed = isPhoneConfirmed;
    }
}

