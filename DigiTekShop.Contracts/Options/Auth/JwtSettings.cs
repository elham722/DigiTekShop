using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Auth
{
    public sealed class JwtSettings
    {
        [Required] public string Issuer { get; init; } = default!;
        [Required] public string Audience { get; init; } = default!;
        [Required, MinLength(32)] public string? Key { get; init; } 
        [Range(1, 1440)] public int AccessTokenExpirationMinutes { get; init; } = 60;
        [Range(1, 3650)] public int RefreshTokenExpirationDays { get; init; } = 30;
    }
}
