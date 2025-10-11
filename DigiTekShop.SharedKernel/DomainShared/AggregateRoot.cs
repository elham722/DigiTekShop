namespace DigiTekShop.SharedKernel.DomainShared;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent @event)
    {
        if (@event is null) return;
        _domainEvents.Add(@event);
    }

    public IReadOnlyCollection<IDomainEvent> PullDomainEvents()
    {
        var snapshot = _domainEvents.ToArray();
        _domainEvents.Clear();
        return snapshot;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected virtual void ValidateState() { }
    protected void EnsureInvariants() => ValidateState();
}