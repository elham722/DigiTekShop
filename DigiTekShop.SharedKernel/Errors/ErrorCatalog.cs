
using Microsoft.AspNetCore.Http;
using static DigiTekShop.SharedKernel.Errors.ErrorCodes;

namespace DigiTekShop.SharedKernel.Errors;

public static class ErrorCatalog
{
    private static readonly Dictionary<string, ErrorInfo> _map = new(StringComparer.Ordinal)
    {
        // Common
        [Common.ValidationFailed] = new(Common.ValidationFailed, StatusCodes.Status422UnprocessableEntity, "Validation failed."),
        [Common.ConcurrencyConflict] = new(Common.ConcurrencyConflict, StatusCodes.Status409Conflict, "Concurrency conflict occurred."),
        [Common.Timeout] = new(Common.Timeout, StatusCodes.Status408RequestTimeout, "The operation timed out."),
        [Common.Forbidden] = new(Common.Forbidden, StatusCodes.Status403Forbidden, "Access to this resource is forbidden."),
        [Common.Unauthorized] = new(Common.Unauthorized, StatusCodes.Status401Unauthorized, "Unauthorized."),
        [Common.NotFound] = new(Common.NotFound, StatusCodes.Status404NotFound, "Resource not found."),
        [Common.Conflict] = new(Common.Conflict, StatusCodes.Status409Conflict, "Conflict."),
        [Common.RateLimitExceeded] = new(Common.RateLimitExceeded, StatusCodes.Status429TooManyRequests, "Too many requests."),
        [Common.InternalError] = new(Common.InternalError, StatusCodes.Status500InternalServerError, "An internal error occurred."),
        [Common.OperationFailed] = new(Common.OperationFailed, StatusCodes.Status400BadRequest, "Operation failed."),

        // Domain
        [Domain.BusinessRuleViolation] = new(Domain.BusinessRuleViolation, StatusCodes.Status409Conflict, "A business rule was violated."),
        [Domain.InvalidOperation] = new(Domain.InvalidOperation, StatusCodes.Status400BadRequest, "Invalid domain operation."),
        [Domain.EntityNotFound] = new(Domain.EntityNotFound, StatusCodes.Status404NotFound, "The entity was not found."),
        [Domain.EntityExists] = new(Domain.EntityExists, StatusCodes.Status409Conflict, "The entity already exists."),

        // Identity
        [Identity.UserNotFound] = new(Identity.UserNotFound, StatusCodes.Status404NotFound, "User not found."),
        [Identity.UserExists] = new(Identity.UserExists, StatusCodes.Status409Conflict, "User already exists."),
        [Identity.InvalidCredentials] = new(Identity.InvalidCredentials, StatusCodes.Status401Unauthorized, "Invalid username or password."),
        [Identity.AccountLocked] = new(Identity.AccountLocked, StatusCodes.Status423Locked, "Your account is locked."),
        [Identity.SignInNotAllowed] = new(Identity.SignInNotAllowed, StatusCodes.Status403Forbidden, "You are not allowed to sign in."),
        [Identity.RequiresTwoFactor] = new(Identity.RequiresTwoFactor, StatusCodes.Status401Unauthorized, "Two-factor authentication is required."),
        [Identity.EmailTaken] = new(Identity.EmailTaken, StatusCodes.Status409Conflict, "Email already registered."),

        [Identity.InvalidEmail] = new(Identity.InvalidEmail, StatusCodes.Status422UnprocessableEntity, "Invalid email address."),
        [Identity.InvalidPhone] = new(Identity.InvalidPhone, StatusCodes.Status422UnprocessableEntity, "Invalid phone number."),
        [Identity.InvalidPassword] = new(Identity.InvalidPassword, StatusCodes.Status422UnprocessableEntity, "Invalid password."),
        [Identity.PasswordTooWeak] = new(Identity.PasswordTooWeak, StatusCodes.Status422UnprocessableEntity, "Password is too weak."),


        [Identity.PasswordResetDisabled] = new(Identity.PasswordResetDisabled, StatusCodes.Status422UnprocessableEntity, "Reset Password is disabled."),
        [Identity.InvalidUserForPasswordReset] = new(Identity.InvalidUserForPasswordReset, StatusCodes.Status422UnprocessableEntity, "Invalid User For Password Reset."),
        [Identity.PasswordResetCooldownActive] = new(Identity.PasswordResetCooldownActive, StatusCodes.Status422UnprocessableEntity, "Too many failed attempts. Please try again later."),
        [Identity.PasswordResetFailed] = new(Identity.PasswordResetFailed, StatusCodes.Status422UnprocessableEntity, "Password reset failed"),

        [Identity.TokenNotFound] = new(Identity.TokenNotFound, StatusCodes.Status404NotFound, "Token not found."),
        [Identity.TokenExpired] = new(Identity.TokenExpired, StatusCodes.Status401Unauthorized, "Token has expired."),
        [Identity.TokenRevoked] = new(Identity.TokenRevoked, StatusCodes.Status401Unauthorized, "Token has been revoked."),
        [Identity.InvalidToken] = new(Identity.InvalidToken, StatusCodes.Status401Unauthorized, "Invalid token."),

        [Identity.MaxActiveDevices] = new(Identity.MaxActiveDevices, StatusCodes.Status429TooManyRequests, "Maximum active devices exceeded."),
        [Identity.MaxTrustedDevices] = new(Identity.MaxTrustedDevices, StatusCodes.Status429TooManyRequests, "Maximum trusted devices exceeded."),
        [Identity.DeviceNotFound] = new(Identity.DeviceNotFound, StatusCodes.Status404NotFound, "Device not found."),
        [Identity.DeviceExists] = new(Identity.DeviceExists, StatusCodes.Status409Conflict, "Device already exists."),
        [Identity.DeviceInactive] = new(Identity.DeviceInactive, StatusCodes.Status423Locked, "Device inactive."),
        [Identity.DeviceNotTrusted] = new(Identity.DeviceNotTrusted, StatusCodes.Status403Forbidden, "Device is not trusted."),
    };

    public static ErrorInfo Resolve(string? code)
    {
        if (code is null) return new(ErrorCodes.Common.OperationFailed, StatusCodes.Status400BadRequest, "Operation failed.");
        return _map.TryGetValue(code, out var info)
            ? info
            : new(code, StatusCodes.Status400BadRequest, "Operation failed."); 
    }
}
