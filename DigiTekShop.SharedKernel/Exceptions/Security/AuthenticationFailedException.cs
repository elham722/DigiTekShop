namespace DigiTekShop.SharedKernel.Exceptions.Security;

public class AuthenticationFailedException : DomainException
{

    #region Simple + InnerExcepion
    public AuthenticationFailedException(string? message)
        : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.AuthenticationFailed),
            DomainErrorCodes.AuthenticationFailed)
    {
    }

    public AuthenticationFailedException(string? message, Exception innerException)
        : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.AuthenticationFailed),
            DomainErrorCodes.AuthenticationFailed,
            innerException)
    {
    }
    #endregion

    #region MetaData + InnerException


    public AuthenticationFailedException(string userName, object id)
        : base(
            $"{userName} with id '{id}' has login failed.",
            DomainErrorCodes.AuthenticationFailed,
            new Dictionary<string, object>
            {
                { "EntityName", userName },
                { "Id", id }
            })
    {
    }

    public AuthenticationFailedException(string userName, object id, Exception innerException)
        : base(
            $"{userName} with id '{id}' has login failed.",
            DomainErrorCodes.AuthenticationFailed,
            innerException,
            new Dictionary<string, object>
            {
                { "EntityName", userName },
                { "Id", id }
            })
    {
    }

    #endregion
}