namespace DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;
public sealed class VerifyOtpRequestDto
{
    public string Phone { get; init; } = default!;
    public string Code { get; init; } = default!;
}
