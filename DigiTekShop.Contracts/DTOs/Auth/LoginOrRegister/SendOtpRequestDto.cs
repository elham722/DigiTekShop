namespace DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;
public sealed class SendOtpRequestDto
{
    public string Phone { get; init; } = default!;    
    public string? DeviceId { get; init; }          
    public bool RememberDevice { get; init; }   
}
