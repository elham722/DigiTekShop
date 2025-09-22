namespace DigiTekShop.SharedKernel.Exceptions.Conflict;
public class ConcurrencyException : DomainException
{
    public ConcurrencyException(object aggregateId, int expectedVersion, int actualVersion)
        : base(
            $"Concurrency conflict for Aggregate '{aggregateId}'. Expected Version: {expectedVersion},but Actual Version is : {actualVersion}.",
            DomainErrorCodes.ConcurrencyConflict,
            new Dictionary<string, object>
            {
                { "AggregateId", aggregateId },
                { "ExpectedVersion", expectedVersion },
                { "ActualVersion", actualVersion }
            })
    {
    }

    public ConcurrencyException(object aggregateId, int expectedVersion, int actualVersion, Exception innerException)
        : base(
            $"Concurrency conflict for Aggregate '{aggregateId}'. Expected Version: {expectedVersion},but Actual Version is : {actualVersion}.",
            DomainErrorCodes.ConcurrencyConflict,
            innerException,
            new Dictionary<string, object>
            {
                { "AggregateId", aggregateId },
                { "ExpectedVersion", expectedVersion },
                { "ActualVersion", actualVersion }
            })
    {
    }
}
