namespace DigiTekShop.Contracts.Auth.ResetPassword;

public record ForgotPasswordRequestDto(
    string Email,
    string? IpAddress = null,
    string? UserAgent = null
);