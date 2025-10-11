#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;

namespace DigiTekShop.SharedKernel.Exceptions.Security;

public sealed class AuthenticationFailedException : DomainException
{
    // 401 با پیام کاتالوگ یا سفارشی
    public AuthenticationFailedException(string? message = null)
        : base(ErrorCodes.Common.UNAUTHORIZED, message) { }

    public AuthenticationFailedException(string userName, object id)
        : base(
            code: ErrorCodes.Common.UNAUTHORIZED,
            message: $"Authentication failed for '{userName}' (Id='{id}').",
            metadata: new Dictionary<string, object>
            {
                ["UserName"] = userName,
                ["Id"] = id
            })
    { }

    public AuthenticationFailedException(string userName, object id, Exception inner)
        : base(
            code: ErrorCodes.Common.UNAUTHORIZED,
            message: $"Authentication failed for '{userName}' (Id='{id}').",
            innerException: inner,
            metadata: new Dictionary<string, object>
            {
                ["UserName"] = userName,
                ["Id"] = id
            })
    { }
}