namespace DigiTekShop.SharedKernel.Exceptions.Conflict;

public class EntityAlreadyExistsException : DomainException
{
    #region Simple + InnerExcepion

    public EntityAlreadyExistsException(string? message)
        : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.EntityAlreadyExists),
            DomainErrorCodes.EntityAlreadyExists)
    {
    }

    public EntityAlreadyExistsException(string? message, Exception innerException)
        : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.EntityAlreadyExists),
            DomainErrorCodes.EntityAlreadyExists,
            innerException)
    {
    }

    #endregion

    #region MetaData + InnerException

    public EntityAlreadyExistsException(string entityName, object entityKey)
        : base(
            $"{entityName} with id '{entityKey}' already exists.",
            DomainErrorCodes.EntityAlreadyExists,
            new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "Id", entityKey }
            })
    {
    }

    public EntityAlreadyExistsException(string entityName, object entityKey, Exception innerException)
        : base(
            $"{entityName} with id '{entityKey}' already exists.",
            DomainErrorCodes.EntityAlreadyExists,
            innerException,
            new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "Id", entityKey }
            })
    {
    }

    #endregion
}