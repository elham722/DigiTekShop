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

        // Device Management Errors
        public const string MaxActiveDevicesExceeded = "MAX_ACTIVE_DEVICES_EXCEEDED";
        public const string MaxTrustedDevicesExceeded = "MAX_TRUSTED_DEVICES_EXCEEDED";
        public const string DeviceNotFound = "DEVICE_NOT_FOUND";
        public const string DeviceAlreadyExists = "DEVICE_ALREADY_EXISTS";
        public const string DeviceInactive = "DEVICE_INACTIVE";
        public const string DeviceNotTrusted = "DEVICE_NOT_TRUSTED";


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

        // Password Reset
        public const string PASSWORD_RESET_DISABLED = "PASSWORD_RESET_DISABLED";
        public const string PASSWORD_RESET_COOLDOWN_ACTIVE = "PASSWORD_RESET_COOLDOWN_ACTIVE";
        public const string PASSWORD_RESET_DAILY_LIMIT_EXCEEDED = "PASSWORD_RESET_DAILY_LIMIT_EXCEEDED";
        public const string PASSWORDS_DO_NOT_MATCH = "PASSWORDS_DO_NOT_MATCH";
        public const string INVALID_RESET_TOKEN = "INVALID_RESET_TOKEN";
        public const string RESET_TOKEN_EXPIRED = "RESET_TOKEN_EXPIRED";
        public const string PASSWORD_RESET_FAILED = "PASSWORD_RESET_FAILED";
        public const string INVALID_USER_FOR_PASSWORD_RESET = "INVALID_USER_FOR_PASSWORD_RESET";

        // Email Confirmation
        public const string EMAIL_CONFIRMATION_DISABLED = "EMAIL_CONFIRMATION_DISABLED";
        public const string EMAIL_CONFIRMATION_REQUIRED = "EMAIL_CONFIRMATION_REQUIRED";
        public const string EMAIL_NOT_CONFIRMED = "EMAIL_NOT_CONFIRMED";
        public const string INVALID_CONFIRMATION_TOKEN = "INVALID_CONFIRMATION_TOKEN";
        public const string CONFIRMATION_TOKEN_EXPIRED = "CONFIRMATION_TOKEN_EXPIRED";
        public const string EMAIL_CONFIRMATION_FAILED = "EMAIL_CONFIRMATION_FAILED";
        public const string EMAIL_CONFIRMATION_COOLDOWN = "EMAIL_CONFIRMATION_COOLDOWN";

        // Authorization & Access Control
        public const string FORBIDDEN = "FORBIDDEN";
        public const string UNAUTHORIZED = "UNAUTHORIZED";
        public const string INSUFFICIENT_PERMISSIONS = "INSUFFICIENT_PERMISSIONS";
        public const string ACCESS_DENIED = "ACCESS_DENIED";

        // Authentication
        public const string AUTHENTICATION_FAILED = "AUTHENTICATION_FAILED";
        public const string INVALID_AUTHENTICATION_HEADERS = "INVALID_AUTHENTICATION_HEADERS";
        public const string MULTI_FACTOR_REQUIRED = "MULTI_FACTOR_REQUIRED";
        public const string ACCOUNT_DISABLED = "ACCOUNT_DISABLED";
        public const string ACCOUNT_SUSPENDED = "ACCOUNT_SUSPENDED";

        // General
        public const string IDENTITY_ERROR = "IDENTITY_ERROR";
        public const string OPERATION_FAILED = "OPERATION_FAILED";
        public const string CONCURRENCY_CONFLICT = "CONCURRENCY_CONFLICT";
    }
}
