namespace DigiTekShop.SharedKernel.DomainShared.Events;

public abstract record DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; init; }
    public string? CorrelationId { get; init; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    protected DomainEvent()
    {
        OccurredOn = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(
        DateTimeOffset? occurredOn,
        string? correlationId = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        OccurredOn = occurredOn ?? DateTimeOffset.UtcNow;
        CorrelationId = correlationId;
        Metadata = metadata;
    }
}