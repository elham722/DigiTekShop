namespace DigiTekShop.SharedKernel.Exceptions.Security;
public class ForbiddenException : DomainException
{
    #region Simple + InnerExcepion

    public ForbiddenException(string? message)
        : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.Forbidden),
            DomainErrorCodes.Forbidden)
    {
    }

    public ForbiddenException(string? message, Exception innerException)
        : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.Forbidden),
            DomainErrorCodes.Forbidden,
            innerException)
    {
    }

    #endregion

    #region MetaData + InnerException


    public ForbiddenException(string action, object userId)
        : base(
            $"User '{userId}' is not authorized to perform '{action}'",
            DomainErrorCodes.Forbidden,
            new Dictionary<string, object>
            {
                { "EntityName", action },
                { "Id", userId }
            })
    {
    }

    public ForbiddenException(string action, object userId, Exception innerException)
        : base(
            $"User '{userId}' is not authorized to perform '{action}'",
            DomainErrorCodes.Forbidden,
            innerException,
            new Dictionary<string, object>
            {
                { "EntityName", action },
                { "Id", userId }
            })
    {
    }

    #endregion
}
