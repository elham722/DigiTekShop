using DigiTekShop.SharedKernel.Errors;

namespace DigiTekShop.SharedKernel.Exceptions.BusinessRole;

public sealed class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string? message = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        : base(ErrorCodes.Domain.BusinessRuleViolation, message, null, metadata) { }

    public BusinessRuleValidationException(string? message, Exception inner,
        IReadOnlyDictionary<string, object>? metadata = null)
        : base(ErrorCodes.Domain.BusinessRuleViolation, message, inner, metadata) { }
}