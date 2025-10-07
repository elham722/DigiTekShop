using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.Auth.TwoFactor
{
    public record TwoFactorTokenResponseDto(string Token, DateTimeOffset ExpiresAt);
}
