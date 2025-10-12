namespace DigiTekShop.Contracts.DTOs.Auth.ResetPassword
{
    public record ChangePasswordRequestDto(Guid UserId, string CurrentPassword, string NewPassword);
}
