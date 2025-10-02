using DigiTekShop.Identity.Exceptions.Common;

namespace DigiTekShop.Identity.Exceptions.PhoneVerification;
    public sealed class MaxAttemptsExceededException : DomainException
    {
        public Guid UserId { get; }
        public int MaxAttempts { get; }

        public MaxAttemptsExceededException(Guid userId, int maxAttempts)
            : base(
                IdentityErrorMessages.GetMessage(IdentityErrorCodes.MAX_ATTEMPTS_EXCEEDED),
                IdentityErrorCodes.MAX_ATTEMPTS_EXCEEDED,
                new Dictionary<string, object> { { "UserId", userId }, { "MaxAttempts", maxAttempts } }
            )
        {
            UserId = userId;
            MaxAttempts = maxAttempts;
        }
    }
