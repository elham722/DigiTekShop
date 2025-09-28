namespace DigiTekShop.Contracts.DTOs.JwtSettings;
   
    public record TokenResponseDto(
        string AccessToken,
        int AccessTokenExpiresInSeconds,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt);


