using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DigiTekShop.SharedKernel.Exceptions.Validation;
public class DomainValidationException: DomainException
{
    public IReadOnlyCollection<string> Errors { get; }

    #region Simple + InnerExcepion

    public DomainValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.", DomainErrorCodes.ValidationFailed)
    {
        Errors = errors.ToList().AsReadOnly();
    }
    public DomainValidationException(IEnumerable<string> errors, Exception innerException)
        : base("One or more validation errors occurred.", DomainErrorCodes.ValidationFailed, innerException)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    #endregion

    #region MetaData + InnerException


    public DomainValidationException(IEnumerable<string> errors, string entityName, object id)
        : base(
            "One or more validation errors occurred.",
            DomainErrorCodes.ValidationFailed,
            new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "Id", id }
            })
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public DomainValidationException(IEnumerable<string> errors, string entityName, object id, Exception innerException)
        : base(
            "One or more validation errors occurred.",
            DomainErrorCodes.ValidationFailed,
            innerException,
            new Dictionary<string, object>
            {
                { "EntityName", entityName },
                { "Id", id }
            })
    {
        Errors = errors.ToList().AsReadOnly();
    }

    #endregion
}
