using System.Net;
using static DigiTekShop.SharedKernel.Errors.ErrorCodes;

namespace DigiTekShop.SharedKernel.Errors;

public static class ErrorCatalog
{
    private static readonly ErrorInfo DefaultBadRequest =
        new(Common.OPERATION_FAILED, (int)HttpStatusCode.BadRequest, "Operation failed.");

    private static readonly Dictionary<string, ErrorInfo> _map = new(StringComparer.Ordinal)
    {
        // Common
        [Common.VALIDATION_FAILED] = new(Common.VALIDATION_FAILED, (int)HttpStatusCode.UnprocessableEntity, "Validation failed."),
        [Common.CONCURRENCY_CONFLICT] = new(Common.CONCURRENCY_CONFLICT, (int)HttpStatusCode.Conflict, "Concurrency conflict occurred."),
        [Common.TIMEOUT] = new(Common.TIMEOUT, (int)HttpStatusCode.RequestTimeout, "The operation timed out."),
        [Common.FORBIDDEN] = new(Common.FORBIDDEN, (int)HttpStatusCode.Forbidden, "Access to this resource is forbidden."),
        [Common.UNAUTHORIZED] = new(Common.UNAUTHORIZED, (int)HttpStatusCode.Unauthorized, "Unauthorized."),
        [Common.NOT_FOUND] = new(Common.NOT_FOUND, (int)HttpStatusCode.NotFound, "Resource not found."),
        [Common.CONFLICT] = new(Common.CONFLICT, (int)HttpStatusCode.Conflict, "Conflict."),
        [Common.RATE_LIMIT_EXCEEDED] = new(Common.RATE_LIMIT_EXCEEDED, (int)HttpStatusCode.TooManyRequests, "Too many requests."),
        [Common.INTERNAL_ERROR] = new(Common.INTERNAL_ERROR, (int)HttpStatusCode.InternalServerError, "An internal error occurred."),
        [Common.OPERATION_FAILED] = DefaultBadRequest,

        // Domain
        [Domain.BUSINESS_RULE_VIOLATION] = new(Domain.BUSINESS_RULE_VIOLATION, (int)HttpStatusCode.Conflict, "A business rule was violated."),
        [Domain.INVALID_OPERATION] = new(Domain.INVALID_OPERATION, (int)HttpStatusCode.BadRequest, "Invalid domain operation."),
        [Domain.ENTITY_NOT_FOUND] = new(Domain.ENTITY_NOT_FOUND, (int)HttpStatusCode.NotFound, "The entity was not found."),
        [Domain.ENTITY_EXISTS] = new(Domain.ENTITY_EXISTS, (int)HttpStatusCode.Conflict, "The entity already exists."),

        // Identity
        [Identity.USER_NOT_FOUND] = new(Identity.USER_NOT_FOUND, (int)HttpStatusCode.NotFound, "User not found."),
        [Identity.USER_EXISTS] = new(Identity.USER_EXISTS, (int)HttpStatusCode.Conflict, "User already exists."),
        [Identity.INVALID_CREDENTIALS] = new(Identity.INVALID_CREDENTIALS, (int)HttpStatusCode.Unauthorized, "Invalid username or password."),
        [Identity.ACCOUNT_LOCKED] = new(Identity.ACCOUNT_LOCKED, (int)HttpStatusCode.Locked, "Your account is locked."),
        [Identity.SIGNIN_NOT_ALLOWED] = new(Identity.SIGNIN_NOT_ALLOWED, (int)HttpStatusCode.Forbidden, "You are not allowed to sign in."),
        [Identity.REQUIRES_TWO_FACTOR] = new(Identity.REQUIRES_TWO_FACTOR, (int)HttpStatusCode.Unauthorized, "Two-factor authentication is required."),
        [Identity.EMAIL_TAKEN] = new(Identity.EMAIL_TAKEN, (int)HttpStatusCode.Conflict, "Email already registered."),
        [Identity.INVALID_EMAIL] = new(Identity.INVALID_EMAIL, (int)HttpStatusCode.UnprocessableEntity, "Invalid email address."),
        [Identity.INVALID_PHONE] = new(Identity.INVALID_PHONE, (int)HttpStatusCode.UnprocessableEntity, "Invalid phone number."),
        [Identity.INVALID_PASSWORD] = new(Identity.INVALID_PASSWORD, (int)HttpStatusCode.UnprocessableEntity, "Invalid password."),
        [Identity.PASSWORD_TOO_WEAK] = new(Identity.PASSWORD_TOO_WEAK, (int)HttpStatusCode.UnprocessableEntity, "Password is too weak."),
        [Identity.PASSWORD_RESET_DISABLED] = new(Identity.PASSWORD_RESET_DISABLED, (int)HttpStatusCode.UnprocessableEntity, "Reset Password is disabled."),
        [Identity.INVALID_USER_FOR_PASSWORD_RESET] = new(Identity.INVALID_USER_FOR_PASSWORD_RESET, (int)HttpStatusCode.UnprocessableEntity, "Invalid user for password reset."),
        [Identity.PASSWORD_RESET_COOLDOWN_ACTIVE] = new(Identity.PASSWORD_RESET_COOLDOWN_ACTIVE, (int)HttpStatusCode.UnprocessableEntity, "Too many failed attempts. Please try again later."),
        [Identity.PASSWORD_RESET_FAILED] = new(Identity.PASSWORD_RESET_FAILED, (int)HttpStatusCode.UnprocessableEntity, "Password reset failed."),
        [Identity.TOKEN_NOT_FOUND] = new(Identity.TOKEN_NOT_FOUND, (int)HttpStatusCode.NotFound, "Token not found."),
        [Identity.TOKEN_EXPIRED] = new(Identity.TOKEN_EXPIRED, (int)HttpStatusCode.Unauthorized, "Token has expired."),
        [Identity.TOKEN_REVOKED] = new(Identity.TOKEN_REVOKED, (int)HttpStatusCode.Unauthorized, "Token has been revoked."),
        [Identity.INVALID_TOKEN] = new(Identity.INVALID_TOKEN, (int)HttpStatusCode.Unauthorized, "Invalid token."),
        [Identity.MAX_ACTIVE_DEVICES_EXCEEDED] = new(Identity.MAX_ACTIVE_DEVICES_EXCEEDED, (int)HttpStatusCode.TooManyRequests, "Maximum active devices exceeded."),
        [Identity.MAX_TRUSTED_DEVICES_EXCEEDED] = new(Identity.MAX_TRUSTED_DEVICES_EXCEEDED, (int)HttpStatusCode.TooManyRequests, "Maximum trusted devices exceeded."),
        [Identity.DEVICE_NOT_FOUND] = new(Identity.DEVICE_NOT_FOUND, (int)HttpStatusCode.NotFound, "Device not found."),
        [Identity.DEVICE_EXISTS] = new(Identity.DEVICE_EXISTS, (int)HttpStatusCode.Conflict, "Device already exists."),
        [Identity.DEVICE_INACTIVE] = new(Identity.DEVICE_INACTIVE, (int)HttpStatusCode.Locked, "Device inactive."),
        [Identity.DEVICE_NOT_TRUSTED] = new(Identity.DEVICE_NOT_TRUSTED, (int)HttpStatusCode.Forbidden, "Device is not trusted."),
    };

    public static ErrorInfo Resolve(string? code)
        => string.IsNullOrWhiteSpace(code)
            ? DefaultBadRequest
            : (_map.TryGetValue(code, out var info) ? info : DefaultBadRequest);
}
