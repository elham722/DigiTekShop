namespace DigiTekShop.SharedKernel.DomainShared.Events;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }   
    IReadOnlyCollection<IDomainEvent> PullDomainEvents();     
}