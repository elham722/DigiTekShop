namespace DigiTekShop.SharedKernel.Exceptions.Validation;
public class InvalidDomainOperationException : DomainException
{
    #region Simple + InnerException

    public InvalidDomainOperationException(string? message) : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.InvalidDomainOperation),
        DomainErrorCodes.InvalidDomainOperation)
    {

    }

    public InvalidDomainOperationException(string? message, Exception innerException) 
        : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.InvalidDomainOperation),
        DomainErrorCodes.InvalidDomainOperation, innerException)
    {

    }

    #endregion

    #region MetaData + InnerException

    public InvalidDomainOperationException(string entityName, object entityId, string? propertyName = null)
        : base(
            propertyName != null
                ? $"{entityName} with id '{entityId}' cannot perform operation on property '{propertyName}'."
                : $"{entityName} with id '{entityId}' cannot perform the requested operation.",
            DomainErrorCodes.InvalidDomainOperation,
            new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "Id", entityId },
                { "Property", propertyName ?? string.Empty }
            })
    {
    }

    public InvalidDomainOperationException(string entityName, object entityId, string? propertyName, Exception innerException)
        : base(
            propertyName != null
                ? $"{entityName} with id '{entityId}' cannot perform operation on property '{propertyName}'."
                : $"{entityName} with id '{entityId}' cannot perform the requested operation.",
            DomainErrorCodes.InvalidDomainOperation,
            innerException,
            new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "Id", entityId },
                { "Property", propertyName ?? string.Empty }
            })
    {
    }

    #endregion
}
