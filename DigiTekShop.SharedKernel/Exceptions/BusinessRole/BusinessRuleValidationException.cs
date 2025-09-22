namespace DigiTekShop.SharedKernel.Exceptions.BusinessRole;
public class BusinessRuleValidationException : DomainException
{
    #region Simple + InnerExcepion

    public BusinessRuleValidationException(string? message) : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.BusinessRuleValidation),
        DomainErrorCodes.BusinessRuleValidation)
    {

    }

    public BusinessRuleValidationException(string? message, Exception innerException)
        : base(message ?? DomainErrorMessages.GetMessage(DomainErrorCodes.BusinessRuleValidation),
        DomainErrorCodes.BusinessRuleValidation, innerException)
    {

    }

    #endregion
}
