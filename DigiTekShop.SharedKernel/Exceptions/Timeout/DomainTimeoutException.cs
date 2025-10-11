#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;

namespace DigiTekShop.SharedKernel.Exceptions.Timeout;

public sealed class DomainTimeoutException : DomainException
{
    public TimeSpan Duration { get; }

    public DomainTimeoutException(TimeSpan duration, string? message = null)
        : base(
            code: ErrorCodes.Common.TIMEOUT,
            message: message ?? $"Operation timed out after {duration.TotalSeconds} seconds.",
            metadata: new Dictionary<string, object> { ["Duration"] = duration })
    {
        Duration = duration;
    }

    public DomainTimeoutException(TimeSpan duration, Exception inner, string? message = null)
        : base(
            code: ErrorCodes.Common.TIMEOUT,
            message: message ?? $"Operation timed out after {duration.TotalSeconds} seconds.",
            innerException: inner,
            metadata: new Dictionary<string, object> { ["Duration"] = duration })
    {
        Duration = duration;
    }
}