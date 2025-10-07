using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.TwoFactor
{
    public record VerifyTwoFactorRequestDto(string UserId, TwoFactorProvider Provider, string Code);
}
