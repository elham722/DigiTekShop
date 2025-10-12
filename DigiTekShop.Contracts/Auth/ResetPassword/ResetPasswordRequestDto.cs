namespace DigiTekShop.Contracts.Auth.ResetPassword;

public record ResetPasswordRequestDto(
    string UserId,
    string Token,
    string NewPassword,
    string? IpAddress = null,
    string? UserAgent = null
);