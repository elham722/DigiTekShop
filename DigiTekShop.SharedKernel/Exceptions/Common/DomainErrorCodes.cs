namespace DigiTekShop.SharedKernel.Exceptions.Common
{
    public static class DomainErrorCodes
    {
        // Business Logic Errors
        public const string BusinessRuleValidation = "BUSINESS_RULE_VALIDATION";
        public const string InvalidDomainOperation = "INVALID_DOMAIN_OPERATION";

        // Entity Errors
        public const string EntityNotFound = "ENTITY_NOT_FOUND";
        public const string EntityAlreadyExists = "ENTITY_ALREADY_EXISTS";

        // Validation Errors
        public const string ValidationFailed = "VALIDATION_FAILED";

        // Concurrency Errors
        public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

        // Security Errors (used by SharedKernal)
        public const string Forbidden = "FORBIDDEN";
        public const string AuthenticationFailed = "AUTHENTICATION_FAILED";

        // System Errors
        public const string Timeout = "TIMEOUT";
        public const string SystemError = "SYSTEM_ERROR";
    }
}