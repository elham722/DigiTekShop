namespace DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;
public sealed class LoginResponseDto
{
    public Guid UserId { get; init; }
    public bool IsNewUser { get; init; }
    public string AccessToken { get; init; } = default!;
    public DateTimeOffset AccessTokenExpiresAtUtc { get; init; }
    public string RefreshToken { get; init; } = default!;
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; init; }
}
