
using DigiTekShop.SharedKernel.Errors;

namespace DigiTekShop.SharedKernel.Exceptions.Conflict;

public sealed class ConcurrencyException : DomainException
{
    public ConcurrencyException(object aggregateId, int expectedVersion, int actualVersion)
        : base(
            code: ErrorCodes.Common.CONCURRENCY_CONFLICT,
            message: $"Concurrency conflict for Aggregate '{aggregateId}'. Expected Version: {expectedVersion}, but Actual Version is: {actualVersion}.",
            metadata: new Dictionary<string, object>
            {
                ["AggregateId"] = aggregateId,
                ["ExpectedVersion"] = expectedVersion,
                ["ActualVersion"] = actualVersion
            })
    { }

    public ConcurrencyException(object aggregateId, int expectedVersion, int actualVersion, Exception inner)
        : base(
            code: ErrorCodes.Common.CONCURRENCY_CONFLICT,
            message: $"Concurrency conflict for Aggregate '{aggregateId}'. Expected Version: {expectedVersion}, but Actual Version is: {actualVersion}.",
            innerException: inner,
            metadata: new Dictionary<string, object>
            {
                ["AggregateId"] = aggregateId,
                ["ExpectedVersion"] = expectedVersion,
                ["ActualVersion"] = actualVersion
            })
    { }

    public ConcurrencyException(object aggregateId, byte[] expectedRowVersion, byte[] actualRowVersion)
        : base(
            code: ErrorCodes.Common.CONCURRENCY_CONFLICT,
            message: $"Concurrency conflict for Aggregate '{aggregateId}'. Expected RV: {Convert.ToHexString(expectedRowVersion)}, Actual RV: {Convert.ToHexString(actualRowVersion)}.",
            metadata: new Dictionary<string, object>
            {
                ["AggregateId"] = aggregateId.ToString(),
                ["ExpectedRowVersion"] = Convert.ToHexString(expectedRowVersion),
                ["ActualRowVersion"] = Convert.ToHexString(actualRowVersion)
            })
    { }

}