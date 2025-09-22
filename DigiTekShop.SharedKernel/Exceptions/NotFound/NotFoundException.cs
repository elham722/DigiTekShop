namespace DigiTekShop.SharedKernel.Exceptions.NotFound;

public class NotFoundException : DomainException
{
    #region Simple + InnerExcepion

    public NotFoundException(string? message) : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.EntityNotFound),
        DomainErrorCodes.EntityNotFound)
    {

    }

    public NotFoundException(string? message, Exception innerException) : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.EntityNotFound),
        DomainErrorCodes.EntityNotFound, innerException)
    {

    }

    #endregion

    #region MetaData + InnerException


    public NotFoundException(string entityName, object id)
        : base(
            $"{entityName} with id '{id}' was not found.",
            DomainErrorCodes.EntityNotFound,
            new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "Id", id }
            })
    {
    }

    public NotFoundException(string entityName, object id, Exception innerException)
        : base(
            $"{entityName} with id '{id}' was not found.",
            DomainErrorCodes.EntityNotFound,
            innerException,
            new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "Id", id }
            })
    {
    }

    #endregion

}