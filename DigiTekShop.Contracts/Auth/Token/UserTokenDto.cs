namespace DigiTekShop.Contracts.Auth.Token;

public record UserTokenDto(string TokenType, DateTime CreatedAt, DateTime ExpiresAt, bool IsRevoked, string? DeviceId, string? IpAddress);


