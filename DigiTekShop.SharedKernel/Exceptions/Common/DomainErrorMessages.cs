namespace DigiTekShop.SharedKernel.Exceptions.Common;
    public static class DomainErrorMessages
    {
        private static readonly Dictionary<string, string> _messages = new()
        {
            { DomainErrorCodes.BusinessRuleValidation, "A business rule was violated." },
            { DomainErrorCodes.InvalidDomainOperation, "Invalid operation in domain." },
            { DomainErrorCodes.EntityNotFound, "The entity was not found." },
            { DomainErrorCodes.EntityAlreadyExists, "The entity already exists." },
            { DomainErrorCodes.ValidationFailed, "Validation failed." },
            { DomainErrorCodes.ConcurrencyConflict, "Concurrency conflict occurred." },
            { DomainErrorCodes.Forbidden, "Access to this resource is forbidden." },
            { DomainErrorCodes.AuthenticationFailed, "Authentication failed." },
            { DomainErrorCodes.Timeout, "The operation timed out." },
            { DomainErrorCodes.SystemError, "A system error occurred." }
        };

    public static string GetMessage(string errorCode)
        => _messages.TryGetValue(errorCode, out var msg) ? msg : "Unknown error.";
}