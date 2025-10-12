namespace DigiTekShop.Contracts.Auth.Login
{
    public record LoginRequestDto(string Email, string Password, string? DeviceId, string? UserAgent, string? Ip);
}
