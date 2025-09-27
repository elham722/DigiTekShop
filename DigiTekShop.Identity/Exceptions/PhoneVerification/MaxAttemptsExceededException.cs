using DigiTekShop.SharedKernel.Exceptions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Exceptions.PhoneVerification
{
    public sealed class MaxAttemptsExceededException : DomainException
    {
        public Guid UserId { get; }
        public int MaxAttempts { get; }

        public MaxAttemptsExceededException(Guid userId, int maxAttempts)
            : base(
                DomainErrorMessages.GetMessage(DomainErrorCodes.VerificationMaxAttemptsExceeded),
                DomainErrorCodes.VerificationMaxAttemptsExceeded,
                new Dictionary<string, object> { { "UserId", userId }, { "MaxAttempts", maxAttempts } }
            )
        {
            UserId = userId;
            MaxAttempts = maxAttempts;
        }
    }
}
