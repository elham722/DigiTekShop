namespace DigiTekShop.SharedKernel.Exceptions.Common;

public class DomainException : Exception
{
    public string? ErrorCode { get; }
    public Dictionary<string, object>? Metadata { get; }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, string errorCode, Dictionary<string, object>? metadata = null) : base(message)
    {
        ErrorCode = errorCode;
        Metadata = metadata;
    }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }

    public DomainException(string message, string errorCode, Exception innerException, Dictionary<string, object>? metadata = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Metadata = metadata;
    }

  
    public static DomainException FromCode(string errorCode)
    {
        var message = DomainErrorMessages.GetMessage(errorCode);
        return new DomainException(message, errorCode);
    }

    public override string ToString()
    {
        var meta = Metadata is { Count: > 0 }
            ? $" Metadata: {string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}"
            : string.Empty;

        return $"{GetType().Name}: {Message} (ErrorCode: {ErrorCode}){meta}";
    }
}