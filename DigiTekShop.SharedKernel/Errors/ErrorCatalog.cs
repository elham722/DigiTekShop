using Microsoft.AspNetCore.Http;
using static DigiTekShop.SharedKernel.Errors.ErrorCodes;

namespace DigiTekShop.SharedKernel.Errors;

public static class ErrorCatalog
{
    private static readonly Dictionary<string, ErrorInfo> _map = new(StringComparer.Ordinal)
    {
        // Common
        [Common.VALIDATION_FAILED] = new(Common.VALIDATION_FAILED, StatusCodes.Status422UnprocessableEntity, "Validation failed."),
        [Common.CONCURRENCY_CONFLICT] = new(Common.CONCURRENCY_CONFLICT, StatusCodes.Status409Conflict, "Concurrency conflict occurred."),
        [Common.TIMEOUT] = new(Common.TIMEOUT, StatusCodes.Status408RequestTimeout, "The operation timed out."),
        [Common.FORBIDDEN] = new(Common.FORBIDDEN, StatusCodes.Status403Forbidden, "Access to this resource is forbidden."),
        [Common.UNAUTHORIZED] = new(Common.UNAUTHORIZED, StatusCodes.Status401Unauthorized, "Unauthorized."),
        [Common.NOT_FOUND] = new(Common.NOT_FOUND, StatusCodes.Status404NotFound, "Resource not found."),
        [Common.CONFLICT] = new(Common.CONFLICT, StatusCodes.Status409Conflict, "Conflict."),
        [Common.RATE_LIMIT_EXCEEDED] = new(Common.RATE_LIMIT_EXCEEDED, StatusCodes.Status429TooManyRequests, "Too many requests."),
        [Common.INTERNAL_ERROR] = new(Common.INTERNAL_ERROR, StatusCodes.Status500InternalServerError, "An internal error occurred."),
        [Common.OPERATION_FAILED] = new(Common.OPERATION_FAILED, StatusCodes.Status400BadRequest, "Operation failed."),

        // Domain
        [Domain.BUSINESS_RULE_VIOLATION] = new(Domain.BUSINESS_RULE_VIOLATION, StatusCodes.Status409Conflict, "A business rule was violated."),
        [Domain.INVALID_OPERATION] = new(Domain.INVALID_OPERATION, StatusCodes.Status400BadRequest, "Invalid domain operation."),
        [Domain.ENTITY_NOT_FOUND] = new(Domain.ENTITY_NOT_FOUND, StatusCodes.Status404NotFound, "The entity was not found."),
        [Domain.ENTITY_EXISTS] = new(Domain.ENTITY_EXISTS, StatusCodes.Status409Conflict, "The entity already exists."),

        // Identity
        [Identity.USER_NOT_FOUND] = new(Identity.USER_NOT_FOUND, StatusCodes.Status404NotFound, "User not found."),
        [Identity.USER_EXISTS] = new(Identity.USER_EXISTS, StatusCodes.Status409Conflict, "User already exists."),
        [Identity.INVALID_CREDENTIALS] = new(Identity.INVALID_CREDENTIALS, StatusCodes.Status401Unauthorized, "Invalid username or password."),
        [Identity.ACCOUNT_LOCKED] = new(Identity.ACCOUNT_LOCKED, StatusCodes.Status423Locked, "Your account is locked."),
        [Identity.SIGNIN_NOT_ALLOWED] = new(Identity.SIGNIN_NOT_ALLOWED, StatusCodes.Status403Forbidden, "You are not allowed to sign in."),
        [Identity.REQUIRES_TWO_FACTOR] = new(Identity.REQUIRES_TWO_FACTOR, StatusCodes.Status401Unauthorized, "Two-factor authentication is required."),
        [Identity.EMAIL_TAKEN] = new(Identity.EMAIL_TAKEN, StatusCodes.Status409Conflict, "Email already registered."),

        [Identity.INVALID_EMAIL] = new(Identity.INVALID_EMAIL, StatusCodes.Status422UnprocessableEntity, "Invalid email address."),
        [Identity.INVALID_PHONE] = new(Identity.INVALID_PHONE, StatusCodes.Status422UnprocessableEntity, "Invalid phone number."),
        [Identity.INVALID_PASSWORD] = new(Identity.INVALID_PASSWORD, StatusCodes.Status422UnprocessableEntity, "Invalid password."),
        [Identity.PASSWORD_TOO_WEAK] = new(Identity.PASSWORD_TOO_WEAK, StatusCodes.Status422UnprocessableEntity, "Password is too weak."),

        [Identity.PASSWORD_RESET_DISABLED] = new(Identity.PASSWORD_RESET_DISABLED, StatusCodes.Status422UnprocessableEntity, "Reset Password is disabled."),
        [Identity.INVALID_USER_FOR_PASSWORD_RESET] = new(Identity.INVALID_USER_FOR_PASSWORD_RESET, StatusCodes.Status422UnprocessableEntity, "Invalid user for password reset."),
        [Identity.PASSWORD_RESET_COOLDOWN_ACTIVE] = new(Identity.PASSWORD_RESET_COOLDOWN_ACTIVE, StatusCodes.Status422UnprocessableEntity, "Too many failed attempts. Please try again later."),
        [Identity.PASSWORD_RESET_FAILED] = new(Identity.PASSWORD_RESET_FAILED, StatusCodes.Status422UnprocessableEntity, "Password reset failed."),

        [Identity.TOKEN_NOT_FOUND] = new(Identity.TOKEN_NOT_FOUND, StatusCodes.Status404NotFound, "Token not found."),
        [Identity.TOKEN_EXPIRED] = new(Identity.TOKEN_EXPIRED, StatusCodes.Status401Unauthorized, "Token has expired."),
        [Identity.TOKEN_REVOKED] = new(Identity.TOKEN_REVOKED, StatusCodes.Status401Unauthorized, "Token has been revoked."),
        [Identity.INVALID_TOKEN] = new(Identity.INVALID_TOKEN, StatusCodes.Status401Unauthorized, "Invalid token."),

        [Identity.MAX_ACTIVE_DEVICES_EXCEEDED] = new(Identity.MAX_ACTIVE_DEVICES_EXCEEDED, StatusCodes.Status429TooManyRequests, "Maximum active devices exceeded."),
        [Identity.MAX_TRUSTED_DEVICES_EXCEEDED] = new(Identity.MAX_TRUSTED_DEVICES_EXCEEDED, StatusCodes.Status429TooManyRequests, "Maximum trusted devices exceeded."),
        [Identity.DEVICE_NOT_FOUND] = new(Identity.DEVICE_NOT_FOUND, StatusCodes.Status404NotFound, "Device not found."),
        [Identity.DEVICE_EXISTS] = new(Identity.DEVICE_EXISTS, StatusCodes.Status409Conflict, "Device already exists."),
        [Identity.DEVICE_INACTIVE] = new(Identity.DEVICE_INACTIVE, StatusCodes.Status423Locked, "Device inactive."),
        [Identity.DEVICE_NOT_TRUSTED] = new(Identity.DEVICE_NOT_TRUSTED, StatusCodes.Status403Forbidden, "Device is not trusted."),
    };

    public static ErrorInfo Resolve(string? code)
    {
        if (code is null)
            return new(ErrorCodes.Common.OPERATION_FAILED, StatusCodes.Status400BadRequest, "Operation failed.");

        return _map.TryGetValue(code, out var info)
            ? info
            : new(code, StatusCodes.Status400BadRequest, "Operation failed.");
    }
}
