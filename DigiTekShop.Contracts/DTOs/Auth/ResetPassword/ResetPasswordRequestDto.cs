namespace DigiTekShop.Contracts.DTOs.Auth.ResetPassword;

public record ResetPasswordRequestDto(string UserId, string Token, string NewPassword);