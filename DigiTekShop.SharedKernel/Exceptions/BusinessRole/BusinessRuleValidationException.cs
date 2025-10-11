using DigiTekShop.SharedKernel.Errors;

namespace DigiTekShop.SharedKernel.Exceptions.BusinessRole;

public sealed class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string? message = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        : base(ErrorCodes.Domain.BUSINESS_RULE_VIOLATION, message, null, metadata) { }

    public BusinessRuleValidationException(string? message, Exception inner,
        IReadOnlyDictionary<string, object>? metadata = null)
        : base(ErrorCodes.Domain.BUSINESS_RULE_VIOLATION, message, inner, metadata) { }
}