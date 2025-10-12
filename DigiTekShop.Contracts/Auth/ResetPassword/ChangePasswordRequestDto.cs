namespace DigiTekShop.Contracts.Auth.ResetPassword
{
    public record ChangePasswordRequestDto(Guid UserId, string CurrentPassword, string NewPassword);
}
