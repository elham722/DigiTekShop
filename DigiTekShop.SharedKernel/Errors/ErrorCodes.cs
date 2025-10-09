namespace DigiTekShop.SharedKernel.Errors;

public static class ErrorCodes
{
    public static class Common
    {
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
        public const string Timeout = "TIMEOUT";
        public const string Forbidden = "FORBIDDEN";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string InternalError = "INTERNAL_ERROR";
        public const string OperationFailed = "OPERATION_FAILED";
    }

    public static class Domain
    {
        public const string BusinessRuleViolation = "BUSINESS_RULE_VALIDATION";
        public const string InvalidOperation = "INVALID_DOMAIN_OPERATION";
        public const string EntityNotFound = "ENTITY_NOT_FOUND";
        public const string EntityExists = "ENTITY_ALREADY_EXISTS";
    }

    public static class Identity
    {
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string UserExists = "USER_ALREADY_EXISTS";
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string AccountLocked = "ACCOUNT_LOCKED";
        public const string SignInNotAllowed = "SIGNIN_NOT_ALLOWED";
        public const string RequiresTwoFactor = "REQUIRES_TWO_FACTOR";
        public const string EmailTaken = "EMAIL_TAKEN";

        public const string InvalidEmail = "INVALID_EMAIL";
        public const string InvalidPhone = "INVALID_PHONE";
        public const string InvalidPassword = "INVALID_PASSWORD";
        public const string PasswordTooWeak = "PASSWORD_TOO_WEAK";


        public const string PasswordResetDisabled = "PASSWORD_RESET_DISABLED";
        public const string InvalidUserForPasswordReset = "INVALID_USER_FOR_PASSWORD_RESET";
        public const string PasswordResetCooldownActive = "PASSWORD_RESET_COOLDOWN_ACTIVE";
        public const string PasswordResetFailed = "PASSWORD_RESET_FAILED";

        public const string TokenNotFound = "TOKEN_NOT_FOUND";
        public const string TokenExpired = "TOKEN_EXPIRED";
        public const string TokenRevoked = "TOKEN_REVOKED";
        public const string InvalidToken = "INVALID_TOKEN";

        public const string MaxActiveDevices = "MAX_ACTIVE_DEVICES_EXCEEDED";
        public const string MaxTrustedDevices = "MAX_TRUSTED_DEVICES_EXCEEDED";
        public const string DeviceNotFound = "DEVICE_NOT_FOUND";
        public const string DeviceExists = "DEVICE_ALREADY_EXISTS";
        public const string DeviceInactive = "DEVICE_INACTIVE";
        public const string DeviceNotTrusted = "DEVICE_NOT_TRUSTED";
    }
}
