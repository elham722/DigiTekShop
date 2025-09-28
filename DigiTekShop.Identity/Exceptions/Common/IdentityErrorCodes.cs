using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Exceptions.Common
{
    public static class IdentityErrorCodes
    {
        // User Management
        public const string USER_NOT_FOUND = "USER_NOT_FOUND";
        public const string USER_ALREADY_EXISTS = "USER_ALREADY_EXISTS";
        public const string USER_ALREADY_ACTIVE = "USER_ALREADY_ACTIVE";
        public const string USER_ALREADY_INACTIVE = "USER_ALREADY_INACTIVE";
        public const string USER_ALREADY_DELETED = "USER_ALREADY_DELETED";
        public const string USER_ALREADY_LINKED_TO_CUSTOMER = "USER_ALREADY_LINKED_TO_CUSTOMER";
        public const string USER_NOT_LINKED_TO_CUSTOMER = "USER_NOT_LINKED_TO_CUSTOMER";

        // Authentication
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
        public const string ACCOUNT_LOCKED = "ACCOUNT_LOCKED";
        public const string SIGNIN_NOT_ALLOWED = "SIGNIN_NOT_ALLOWED";
        public const string REQUIRES_TWO_FACTOR = "REQUIRES_TWO_FACTOR";
        public const string INVALID_LOGIN = "INVALID_LOGIN";

        // Role Management
        public const string ROLE_NOT_FOUND = "ROLE_NOT_FOUND";
        public const string ROLE_ALREADY_EXISTS = "ROLE_ALREADY_EXISTS";
        public const string USER_ALREADY_IN_ROLE = "USER_ALREADY_IN_ROLE";
        public const string USER_NOT_IN_ROLE = "USER_NOT_IN_ROLE";

        // Permission Management
        public const string PERMISSION_NOT_FOUND = "PERMISSION_NOT_FOUND";
        public const string PERMISSION_ALREADY_EXISTS = "PERMISSION_ALREADY_EXISTS";
        public const string PERMISSION_ALREADY_GRANTED = "PERMISSION_ALREADY_GRANTED";
        public const string PERMISSION_NOT_GRANTED = "PERMISSION_NOT_GRANTED";

        // Device Management
        public const string DEVICE_NOT_FOUND = "DEVICE_NOT_FOUND";
        public const string DEVICE_ALREADY_EXISTS = "DEVICE_ALREADY_EXISTS";
        public const string DEVICE_ALREADY_TRUSTED = "DEVICE_ALREADY_TRUSTED";
        public const string DEVICE_NOT_TRUSTED = "DEVICE_NOT_TRUSTED";

        // Token Management
        public const string TOKEN_NOT_FOUND = "TOKEN_NOT_FOUND";
        public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";
        public const string TOKEN_REVOKED = "TOKEN_REVOKED";
        public const string TOKEN_ALREADY_REVOKED = "TOKEN_ALREADY_REVOKED";
        public const string INVALID_TOKEN = "INVALID_TOKEN";

        // Phone Verification
        public const string VERIFICATION_CODE_NOT_FOUND = "VERIFICATION_CODE_NOT_FOUND";
        public const string VERIFICATION_CODE_EXPIRED = "VERIFICATION_CODE_EXPIRED";
        public const string VERIFICATION_CODE_INVALID = "VERIFICATION_CODE_INVALID";
        public const string MAX_ATTEMPTS_EXCEEDED = "MAX_ATTEMPTS_EXCEEDED";
        public const string PHONE_ALREADY_VERIFIED = "PHONE_ALREADY_VERIFIED";

        // Validation
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";
        public const string INVALID_EMAIL = "INVALID_EMAIL";
        public const string INVALID_PHONE = "INVALID_PHONE";
        public const string INVALID_PASSWORD = "INVALID_PASSWORD";
        public const string PASSWORD_TOO_WEAK = "PASSWORD_TOO_WEAK";

        // General
        public const string IDENTITY_ERROR = "IDENTITY_ERROR";
        public const string OPERATION_FAILED = "OPERATION_FAILED";
        public const string UNAUTHORIZED = "UNAUTHORIZED";
        public const string FORBIDDEN = "FORBIDDEN";
        public const string CONCURRENCY_CONFLICT = "CONCURRENCY_CONFLICT";
    }
}
