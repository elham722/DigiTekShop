#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;

namespace DigiTekShop.SharedKernel.Exceptions.Security;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string? message = null)
        : base(ErrorCodes.Common.FORBIDDEN, message) { }

    public ForbiddenException(string action, object userId)
        : base(
            code: ErrorCodes.Common.FORBIDDEN,
            message: $"User '{userId}' is not authorized to perform '{action}'.",
            metadata: new Dictionary<string, object>
            {
                ["Action"] = action,
                ["UserId"] = userId
            })
    { }

    public ForbiddenException(string action, object userId, Exception inner)
        : base(
            code: ErrorCodes.Common.FORBIDDEN,
            message: $"User '{userId}' is not authorized to perform '{action}'.",
            innerException: inner,
            metadata: new Dictionary<string, object>
            {
                ["Action"] = action,
                ["UserId"] = userId
            })
    { }
}