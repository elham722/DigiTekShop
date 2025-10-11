#nullable enable
using DigiTekShop.SharedKernel.Errors;

namespace DigiTekShop.SharedKernel.Exceptions.Conflict;

public sealed class EntityAlreadyExistsException : DomainException
{
    // پیام سفارشی یا پیش‌فرض از ErrorCatalog
    public EntityAlreadyExistsException(string? message = null)
        : base(ErrorCodes.Domain.ENTITY_EXISTS, message) { }

    public EntityAlreadyExistsException(string entityName, object entityKey)
        : base(
            code: ErrorCodes.Domain.ENTITY_EXISTS,
            message: $"{entityName} with id '{entityKey}' already exists.",
            metadata: new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["Id"] = entityKey
            })
    { }

    public EntityAlreadyExistsException(string entityName, object entityKey, Exception inner)
        : base(
            code: ErrorCodes.Domain.ENTITY_EXISTS,
            message: $"{entityName} with id '{entityKey}' already exists.",
            innerException: inner,
            metadata: new Dictionary<string, object>
            {
                ["EntityName"] = entityName,
                ["Id"] = entityKey
            })
    { }
}