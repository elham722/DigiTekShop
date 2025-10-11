namespace DigiTekShop.SharedKernel.DomainShared;

public abstract class DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}