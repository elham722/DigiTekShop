using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Guards;

namespace DigiTekShop.SharedKernel.Exceptions.Common;

public class DomainException : Exception
{
    public string Code { get; }
    public IReadOnlyDictionary<string, object>? Metadata { get; }
    public ErrorInfo Info => ErrorCatalog.Resolve(Code);

    protected DomainException(
        string code,
        string? message = null,
        Exception? innerException = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        : base(message ?? ErrorCatalog.Resolve(code).DefaultMessage, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Guard.AgainstNullOrEmpty(code, nameof(code));
        }
        
        Code = code;
        Metadata = metadata;
    }

    public override string ToString()
    {
        var meta = (Metadata is { Count: > 0 })
            ? $" | Meta: {string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}"
            : string.Empty;

        return $"{GetType().Name} [{Code}] {Message}{meta}";
    }
}