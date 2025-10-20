namespace DigiTekShop.Contracts.DTOs.Auth.Login
{
    public record LoginRequestDto(string Email, string Password,bool RememberMe);
}
