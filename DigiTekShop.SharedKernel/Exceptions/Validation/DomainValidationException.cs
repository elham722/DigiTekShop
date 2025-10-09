#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;

namespace DigiTekShop.SharedKernel.Exceptions.Validation;

public sealed class DomainValidationException : DomainException
{
    public IReadOnlyCollection<string> Errors { get; }

    public DomainValidationException(IEnumerable<string> errors, string? message = null)
        : base(
            code: ErrorCodes.Common.ValidationFailed,
            message: message ?? "One or more validation errors occurred.")
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public DomainValidationException(IEnumerable<string> errors, Exception inner, string? message = null)
        : base(
            code: ErrorCodes.Common.ValidationFailed,
            message: message ?? "One or more validation errors occurred.",
            innerException: inner)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public DomainValidationException(IEnumerable<string> errors, string entityName, object id, Exception? inner = null)
        : base(
            code: ErrorCodes.Common.ValidationFailed,
            message: "One or more validation errors occurred.",
            innerException: inner,
            metadata: new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["Id"] = id
            })
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public DomainValidationException(IEnumerable<string> errors, string propertyName, object? currentValue)
        : base(
            code: ErrorCodes.Common.ValidationFailed,
            message: "One or more validation errors occurred.",
            metadata: BuildPropertyMetadata(propertyName, currentValue))
    {
        Errors = errors.ToList().AsReadOnly();
    }

    private static IReadOnlyDictionary<string, object> BuildPropertyMetadata(string propertyName, object? currentValue)
    {
        var meta = new Dictionary<string, object> { ["PropertyName"] = propertyName };
        if (currentValue is not null) meta["CurrentValue"] = currentValue;
        return meta;
    }
}
