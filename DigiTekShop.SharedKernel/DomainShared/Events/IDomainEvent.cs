namespace DigiTekShop.SharedKernel.DomainShared.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
    string? CorrelationId { get; }
    IReadOnlyDictionary<string, object?>? Metadata { get; }
}