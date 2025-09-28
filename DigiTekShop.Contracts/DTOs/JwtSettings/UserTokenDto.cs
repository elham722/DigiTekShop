namespace DigiTekShop.Contracts.DTOs.JwtSettings;

public record UserTokenDto(string TokenType, DateTime CreatedAt, DateTime ExpiresAt, bool IsRevoked, string? DeviceId, string? IpAddress);


