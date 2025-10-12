namespace DigiTekShop.Contracts.DTOs.Auth.Token
{
    public record RefreshRequestDto(string RefreshToken, string? DeviceId, string? Ip, string? UserAgent);
}
