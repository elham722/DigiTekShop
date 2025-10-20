using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.Contracts.Options.Token
{
    public sealed class JwtSettings
    {
        [Required, MinLength(32)] public string? Key { get; init; }
        [Required] public string Issuer { get; init; } = default!;
        [Required] public string Audience { get; init; } = default!;
        [Range(1, 1440)] public int AccessTokenExpirationMinutes { get; init; } = 60;
        [Range(1, 3650)] public int RefreshTokenExpirationDays { get; init; } = 30;
        public string RefreshTokenHashSecret { get; init; } = string.Empty;
    }
}
