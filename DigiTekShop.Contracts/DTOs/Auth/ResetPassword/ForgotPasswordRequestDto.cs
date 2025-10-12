namespace DigiTekShop.Contracts.DTOs.Auth.ResetPassword;

public record ForgotPasswordRequestDto(
    string Email,
    string? IpAddress = null,
    string? UserAgent = null
);