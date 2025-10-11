namespace DigiTekShop.SharedKernel.DomainShared;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> PullDomainEvents();
}