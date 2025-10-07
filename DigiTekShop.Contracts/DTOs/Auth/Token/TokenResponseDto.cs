namespace DigiTekShop.Contracts.DTOs.Auth.Token;

public record TokenResponseDto(
    string TokenType,                
    string AccessToken,
    int ExpiresIn,                    
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt
);

