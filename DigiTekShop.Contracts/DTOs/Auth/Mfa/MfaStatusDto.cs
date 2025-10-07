using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.Mfa
{
    public record MfaStatusDto(
        bool IsEnabled,
        bool IsLocked = false,
        int AttemptCount = 0,
        DateTime? LockedUntil = null,
        DateTime? LastVerifiedAt = null
    );
}
