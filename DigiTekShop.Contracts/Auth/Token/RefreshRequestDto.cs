namespace DigiTekShop.Contracts.Auth.Token
{
    public record RefreshRequestDto(string RefreshToken, string? DeviceId, string? Ip, string? UserAgent);
}
