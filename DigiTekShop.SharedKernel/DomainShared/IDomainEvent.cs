namespace DigiTekShop.SharedKernel.DomainShared;

public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}