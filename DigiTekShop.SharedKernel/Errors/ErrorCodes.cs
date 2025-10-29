namespace DigiTekShop.SharedKernel.Errors;

public static class ErrorCodes
{
    public static class Common
    {
        public const string VALIDATION_FAILED = "VALIDATION_FAILED";
        public const string CONCURRENCY_CONFLICT = "CONCURRENCY_CONFLICT";
        public const string TIMEOUT = "TIMEOUT";
        public const string FORBIDDEN = "FORBIDDEN";
        public const string UNAUTHORIZED = "UNAUTHORIZED";
        public const string NOT_FOUND = "NOT_FOUND";
        public const string CONFLICT = "CONFLICT";
        public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";
        public const string OPERATION_FAILED = "OPERATION_FAILED";
    }

    public static class Domain
    {
        public const string BUSINESS_RULE_VIOLATION = "BUSINESS_RULE_VIOLATION";
        public const string INVALID_OPERATION = "INVALID_DOMAIN_OPERATION";
        public const string ENTITY_NOT_FOUND = "ENTITY_NOT_FOUND";
        public const string ENTITY_EXISTS = "ENTITY_ALREADY_EXISTS";
    }

    public static class Identity
    {
        public const string USER_NOT_FOUND = "USER_NOT_FOUND";
        public const string USER_EXISTS = "USER_ALREADY_EXISTS";
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
        public const string ACCOUNT_LOCKED = "ACCOUNT_LOCKED";
        public const string SIGNIN_NOT_ALLOWED = "SIGNIN_NOT_ALLOWED";
        public const string REQUIRES_TWO_FACTOR = "REQUIRES_TWO_FACTOR";
        public const string EMAIL_TAKEN = "EMAIL_TAKEN";
        public const string INVALID_EMAIL = "INVALID_EMAIL";
        public const string INVALID_PHONE = "INVALID_PHONE";
        public const string INVALID_PASSWORD = "INVALID_PASSWORD";
        public const string PASSWORD_TOO_WEAK = "PASSWORD_TOO_WEAK";
        public const string PASSWORD_RESET_DISABLED = "PASSWORD_RESET_DISABLED";
        public const string INVALID_USER_FOR_PASSWORD_RESET = "INVALID_USER_FOR_PASSWORD_RESET";
        public const string PASSWORD_RESET_COOLDOWN_ACTIVE = "PASSWORD_RESET_COOLDOWN_ACTIVE";
        public const string PASSWORD_RESET_FAILED = "PASSWORD_RESET_FAILED";
        public const string TOKEN_NOT_FOUND = "TOKEN_NOT_FOUND";
        public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";
        public const string TOKEN_REVOKED = "TOKEN_REVOKED";
        public const string INVALID_TOKEN = "INVALID_TOKEN";
        public const string MAX_ACTIVE_DEVICES_EXCEEDED = "MAX_ACTIVE_DEVICES_EXCEEDED";
        public const string MAX_TRUSTED_DEVICES_EXCEEDED = "MAX_TRUSTED_DEVICES_EXCEEDED";
        public const string DEVICE_NOT_FOUND = "DEVICE_NOT_FOUND";
        public const string DEVICE_EXISTS = "DEVICE_ALREADY_EXISTS";
        public const string DEVICE_INACTIVE = "DEVICE_INACTIVE";
        public const string DEVICE_NOT_TRUSTED = "DEVICE_NOT_TRUSTED";
    }

    public static class Otp
    {
        public const string OTP_SEND_RATE_LIMITED = "OTP_SEND_RATE_LIMITED";   // 429
        public const string OTP_VERIFY_RATE_LIMITED = "OTP_VERIFY_RATE_LIMITED"; // 429
        public const string OTP_INVALID = "OTP_INVALID";             // 422
        public const string OTP_EXPIRED = "OTP_EXPIRED";             // 422
        public const string OTP_ALREADY_VERIFIED = "OTP_ALREADY_VERIFIED";    // 409
        public const string OTP_NOT_REQUESTED = "OTP_NOT_REQUESTED";       // 422 (یا 400)
    }
}
