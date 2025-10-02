using System.Collections.Generic;

namespace DigiTekShop.Identity.Exceptions.Common
{
    public static class IdentityErrorMessages
    {
        private static readonly Dictionary<string, string> _messages = new()
        {
            // User Management
            { IdentityErrorCodes.USER_NOT_FOUND, "User not found." },
            { IdentityErrorCodes.USER_ALREADY_EXISTS, "User already exists." },
            { IdentityErrorCodes.USER_ALREADY_ACTIVE, "User is already active." },
            { IdentityErrorCodes.USER_ALREADY_INACTIVE, "User is already inactive." },
            { IdentityErrorCodes.USER_ALREADY_DELETED, "User is already deleted." },
            { IdentityErrorCodes.USER_ALREADY_LINKED_TO_CUSTOMER, "User is already linked to a customer." },
            { IdentityErrorCodes.USER_NOT_LINKED_TO_CUSTOMER, "User is not linked to any customer." },

            // Authentication
            { IdentityErrorCodes.INVALID_CREDENTIALS, "Invalid username or password." },
            { IdentityErrorCodes.ACCOUNT_LOCKED, "Your account is locked." },
            { IdentityErrorCodes.SIGNIN_NOT_ALLOWED, "You are not allowed to sign in." },
            { IdentityErrorCodes.REQUIRES_TWO_FACTOR, "Two-factor authentication is required." },
            { IdentityErrorCodes.INVALID_LOGIN, "Login attempt failed." },

            // Role Management
            { IdentityErrorCodes.ROLE_NOT_FOUND, "Role not found." },
            { IdentityErrorCodes.ROLE_ALREADY_EXISTS, "Role already exists." },
            { IdentityErrorCodes.USER_ALREADY_IN_ROLE, "User is already in this role." },
            { IdentityErrorCodes.USER_NOT_IN_ROLE, "User is not in this role." },

            // Permission Management
            { IdentityErrorCodes.PERMISSION_NOT_FOUND, "Permission not found." },
            { IdentityErrorCodes.PERMISSION_ALREADY_EXISTS, "Permission already exists." },
            { IdentityErrorCodes.PERMISSION_ALREADY_GRANTED, "Permission is already granted." },
            { IdentityErrorCodes.PERMISSION_NOT_GRANTED, "Permission is not granted." },

            // Device Management
            { IdentityErrorCodes.DEVICE_NOT_FOUND, "Device not found." },
            { IdentityErrorCodes.DEVICE_ALREADY_EXISTS, "Device already exists." },
            { IdentityErrorCodes.DEVICE_ALREADY_TRUSTED, "Device is already trusted." },
            { IdentityErrorCodes.DEVICE_NOT_TRUSTED, "Device is not trusted." },

            // Token Management
            { IdentityErrorCodes.TOKEN_NOT_FOUND, "Token not found." },
            { IdentityErrorCodes.TOKEN_EXPIRED, "Token has expired." },
            { IdentityErrorCodes.TOKEN_REVOKED, "Token has been revoked." },
            { IdentityErrorCodes.TOKEN_ALREADY_REVOKED, "Token is already revoked." },
            { IdentityErrorCodes.INVALID_TOKEN, "Invalid token." },

            // Phone Verification
            { IdentityErrorCodes.VERIFICATION_CODE_NOT_FOUND, "Verification code not found." },
            { IdentityErrorCodes.VERIFICATION_CODE_EXPIRED, "Verification code has expired." },
            { IdentityErrorCodes.VERIFICATION_CODE_INVALID, "Verification code is invalid." },
            { IdentityErrorCodes.MAX_ATTEMPTS_EXCEEDED, "Maximum verification attempts exceeded." },
            { IdentityErrorCodes.PHONE_ALREADY_VERIFIED, "Phone number is already verified." },

            // Validation
            { IdentityErrorCodes.VALIDATION_ERROR, "Validation error occurred." },
            { IdentityErrorCodes.INVALID_EMAIL, "Invalid email address." },
            { IdentityErrorCodes.INVALID_PHONE, "Invalid phone number." },
            { IdentityErrorCodes.INVALID_PASSWORD, "Invalid password." },
            { IdentityErrorCodes.PASSWORD_TOO_WEAK, "Password is too weak." },

            // Password Reset
            { IdentityErrorCodes.PASSWORD_RESET_DISABLED, "Password reset is currently disabled." },
            { IdentityErrorCodes.PASSWORD_RESET_COOLDOWN_ACTIVE, "Please wait before requesting another password reset." },
            { IdentityErrorCodes.PASSWORD_RESET_DAILY_LIMIT_EXCEEDED, "You have exceeded the daily password reset limit." },
            { IdentityErrorCodes.PASSWORDS_DO_NOT_MATCH, "The passwords do not match." },
            { IdentityErrorCodes.INVALID_RESET_TOKEN, "The reset token is invalid." },
            { IdentityErrorCodes.RESET_TOKEN_EXPIRED, "The reset token has expired." },
            { IdentityErrorCodes.PASSWORD_RESET_FAILED, "Password reset failed." },
            { IdentityErrorCodes.INVALID_USER_FOR_PASSWORD_RESET, "Invalid user for password reset." },

            // Email Confirmation
            { IdentityErrorCodes.EMAIL_CONFIRMATION_DISABLED, "Email confirmation is currently disabled." },
            { IdentityErrorCodes.EMAIL_CONFIRMATION_REQUIRED, "Email confirmation is required." },
            { IdentityErrorCodes.EMAIL_NOT_CONFIRMED, "Email address is not confirmed." },
            { IdentityErrorCodes.INVALID_CONFIRMATION_TOKEN, "The confirmation token is invalid." },
            { IdentityErrorCodes.CONFIRMATION_TOKEN_EXPIRED, "The confirmation token has expired." },
            { IdentityErrorCodes.EMAIL_CONFIRMATION_FAILED, "Email confirmation failed." },
            { IdentityErrorCodes.EMAIL_CONFIRMATION_COOLDOWN, "Please wait before requesting another email confirmation." },

            // Authorization & Access Control
            { IdentityErrorCodes.FORBIDDEN, "Access to this resource is forbidden." },
            { IdentityErrorCodes.UNAUTHORIZED, "You are not authorized to perform this action." },
            { IdentityErrorCodes.INSUFFICIENT_PERMISSIONS, "You have insufficient permissions for this action." },
            { IdentityErrorCodes.ACCESS_DENIED, "Access denied." },

            // Authentication
            { IdentityErrorCodes.AUTHENTICATION_FAILED, "Authentication failed." },
            { IdentityErrorCodes.INVALID_AUTHENTICATION_HEADERS, "Invalid authentication headers." },
            { IdentityErrorCodes.MULTI_FACTOR_REQUIRED, "Multi-factor authentication is required." },
            { IdentityErrorCodes.ACCOUNT_DISABLED, "Your account has been disabled." },
            { IdentityErrorCodes.ACCOUNT_SUSPENDED, "Your account has been suspended." },

            // General
            { IdentityErrorCodes.IDENTITY_ERROR, "An identity error occurred." },
            { IdentityErrorCodes.OPERATION_FAILED, "Operation failed." },
            { IdentityErrorCodes.CONCURRENCY_CONFLICT, "A concurrency conflict occurred." }
        };

        public static string GetMessage(string errorCode)
        {
            return _messages.TryGetValue(errorCode, out var message)
                ? message
                : "An unknown identity error occurred.";
        }
    }
}
