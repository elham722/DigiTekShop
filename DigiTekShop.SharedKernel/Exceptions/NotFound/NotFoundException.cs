#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;

namespace DigiTekShop.SharedKernel.Exceptions.NotFound;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string? message = null)
        : base(ErrorCodes.Domain.ENTITY_NOT_FOUND, message) { }

    public NotFoundException(string entityName, object id)
        : base(
            code: ErrorCodes.Domain.ENTITY_NOT_FOUND,
            message: $"{entityName} with id '{id}' was not found.",
            metadata: new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["Id"] = id
            })
    { }

    public NotFoundException(string entityName, object id, Exception inner)
        : base(
            code: ErrorCodes.Domain.ENTITY_NOT_FOUND,
            message: $"{entityName} with id '{id}' was not found.",
            innerException: inner,
            metadata: new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["Id"] = id
            })
    { }
}