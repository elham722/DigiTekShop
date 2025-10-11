#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;

namespace DigiTekShop.SharedKernel.Exceptions.Validation;

public sealed class InvalidDomainOperationException : DomainException
{
    public InvalidDomainOperationException(string? message = null)
        : base(ErrorCodes.Domain.INVALID_OPERATION, message) { }

    public InvalidDomainOperationException(string entityName, object entityId, string? propertyName = null, Exception? inner = null)
        : base(
            code: ErrorCodes.Domain.INVALID_OPERATION,
            message: propertyName is null
                ? $"{entityName} with id '{entityId}' cannot perform the requested operation."
                : $"{entityName} with id '{entityId}' cannot perform operation on property '{propertyName}'.",
            innerException: inner,
            metadata: new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["Id"] = entityId,
                ["Property"] = propertyName ?? string.Empty
            })
    { }
}