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

    public DomainValidationException(IEnumerable<string> errors, string propertyName, object? currentValue)
        : base(
            "One or more validation errors occurred.",
            DomainErrorCodes.ValidationFailed,
            BuildPropertyMetadata(propertyName, currentValue))
    {
        Errors = errors.ToList().AsReadOnly();
    }

    private static Dictionary<string, object> BuildPropertyMetadata(string propertyName, object? currentValue)
    {
        var metadata = new Dictionary<string, object>
        {
            { "PropertyName", propertyName }
        };

        if (currentValue is not null)
        {
            metadata["CurrentValue"] = currentValue;
        }

        return metadata;
    }


    #endregion
}
