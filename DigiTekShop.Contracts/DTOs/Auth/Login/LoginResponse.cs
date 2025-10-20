namespace DigiTekShop.Contracts.DTOs.Auth.Login;

public sealed record LoginResponse
{
    public string AccessToken { get; init; } = default!;

    public string RefreshToken { get; init; } = default!;

    public string TokenType { get; init; } = "Bearer";

    public int ExpiresIn { get; init; }

    public DateTime IssuedAtUtc { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public Guid UserId { get; init; }

    public DateTimeOffset? DeviceTrustedUntilUtc { get; init; }

    public string? ClaimsVersion { get; init; }
}