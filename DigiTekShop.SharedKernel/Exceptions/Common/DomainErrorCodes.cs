namespace DigiTekShop.SharedKernel.Exceptions.Common
{
    public static class DomainErrorCodes
    {
        // Business Logic Errors
        public const string BusinessRuleValidation = "BUSINESS_RULE_VALIDATION";
        public const string InvalidDomainOperation = "INVALID_DOMAIN_OPERATION";

        // Entity Errors
        public const string EntityNotFound = "ENTITY_NOT_FOUND";

        // Validation Errors
        public const string ValidationFailed = "VALIDATION_FAILED";

        // Concurrency Errors
        public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
        public const string EntityAlreadyExists = "ENTITY_ALREADY_EXISTS";

        // Authorization Errors
        public const string Forbidden = "FORBIDDEN";
        public const string AuthenticationFailed = "AuthenticationFailed";

        // System Errors
        public const string Timeout = "TIMEOUT";
    }
}