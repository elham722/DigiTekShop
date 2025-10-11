namespace DigiTekShop.SharedKernel.DomainShared.Events;

public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent(
        DateTimeOffset? occurredOn = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        OccurredOn = occurredOn ?? DateTimeOffset.UtcNow;
        CorrelationId = correlationId;
        Metadata = metadata;
    }

    public DateTimeOffset OccurredOn { get; }
    public string? CorrelationId { get; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; }
}