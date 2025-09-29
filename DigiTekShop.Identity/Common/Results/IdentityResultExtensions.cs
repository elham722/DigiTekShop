using DigiTekShop.Identity.Exceptions.Common;
using Microsoft.AspNetCore.Identity;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Identity.Common.Results
{
    public static class IdentityResultExtensions
    {
        public static Result ToResult(this IdentityResult identityResult)
        {
            if (identityResult.Succeeded)
                return Result.Success();

            var errors = identityResult.Errors.Select(e => e.Description).ToList();
            return Result.Failure(errors, IdentityErrorCodes.IDENTITY_ERROR);
        }

        public static Result<T> ToResult<T>(this IdentityResult identityResult, T value)
        {
            if (identityResult.Succeeded)
                return Result<T>.Success(value);

            var errors = identityResult.Errors.Select(e => e.Description).ToList();
            return Result<T>.Failure(errors, IdentityErrorCodes.IDENTITY_ERROR);
        }

        public static Result ToResult(this SignInResult signInResult)
        {
            if (signInResult.Succeeded)
                return Result.Success();

            if (signInResult.IsLockedOut)
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.ACCOUNT_LOCKED), IdentityErrorCodes.ACCOUNT_LOCKED);

            if (signInResult.IsNotAllowed)
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.SIGNIN_NOT_ALLOWED), IdentityErrorCodes.SIGNIN_NOT_ALLOWED);

            if (signInResult.RequiresTwoFactor)
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.REQUIRES_TWO_FACTOR), IdentityErrorCodes.REQUIRES_TWO_FACTOR);

            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_LOGIN), IdentityErrorCodes.INVALID_LOGIN);
        }
    }
}