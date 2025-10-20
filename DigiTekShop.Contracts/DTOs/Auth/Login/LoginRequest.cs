using System.Text.Json.Serialization;

namespace DigiTekShop.Contracts.DTOs.Auth.Login;

public sealed record LoginRequest
{
    public string? Login { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Password { get; init; }

    public bool RememberMe { get; init; }

    public string? TotpCode { get; init; }

    public string? CaptchaToken { get; init; }
}