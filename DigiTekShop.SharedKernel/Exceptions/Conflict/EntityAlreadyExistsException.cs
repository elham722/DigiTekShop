#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;

namespace DigiTekShop.SharedKernel.Exceptions.Conflict;

public sealed class EntityAlreadyExistsException : DomainException
{
    // پیام سفارشی یا پیش‌فرض از ErrorCatalog
    public EntityAlreadyExistsException(string? message = null)
        : base(ErrorCodes.Domain.EntityExists, message) { }

    public EntityAlreadyExistsException(string entityName, object entityKey)
        : base(
            code: ErrorCodes.Domain.EntityExists,
            message: $"{entityName} with id '{entityKey}' already exists.",
            metadata: new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["Id"] = entityKey
            })
    { }

    public EntityAlreadyExistsException(string entityName, object entityKey, Exception inner)
        : base(
            code: ErrorCodes.Domain.EntityExists,
            message: $"{entityName} with id '{entityKey}' already exists.",
            innerException: inner,
            metadata: new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["Id"] = entityKey
            })
    { }
}