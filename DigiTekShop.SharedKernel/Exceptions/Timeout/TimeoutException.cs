namespace DigiTekShop.SharedKernel.Exceptions.Timeout;

public class TimeoutException : DomainException
{
    public TimeSpan Duration { get; }

    #region duration + message

    public TimeoutException(TimeSpan duration)
        : base($"Operation timed out after {duration.TotalSeconds} seconds.",
            DomainErrorCodes.Timeout,
            new Dictionary<string, object> { { "Duration", duration } })
    {
        Duration = duration;
    }

    public TimeoutException(TimeSpan duration, string? message)
        : base(message ?? $"Operation timed out after {duration.TotalSeconds} seconds.",
            DomainErrorCodes.Timeout,
            new Dictionary<string, object> { { "Duration", duration } })
    {
        Duration = duration;
    }

    #endregion

    #region duration + message + innerException


    public TimeoutException(TimeSpan duration, Exception innerException)
        : base($"Operation timed out after {duration.TotalSeconds} seconds.",
            DomainErrorCodes.Timeout,
            innerException,
            new Dictionary<string, object> { { "Duration", duration } })
    {
        Duration = duration;
    }

    public TimeoutException(TimeSpan duration, string? message, Exception innerException)
        : base(message ?? $"Operation timed out after {duration.TotalSeconds} seconds.",
            DomainErrorCodes.Timeout,
            innerException,
            new Dictionary<string, object> { { "Duration", duration } })
    {
        Duration = duration;
    }

    #endregion
}