namespace DigiTekShop.Contracts.DTOs.Auth.JwtSettings;

public class JwtSettings
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 60;
    public int RefreshTokenExpirationDays { get; init; } = 30;
    public string RefreshTokenHashSecret { get; init; } = string.Empty;
}

