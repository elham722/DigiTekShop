namespace DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation
{
    public record ConfirmEmailRequestDto(Guid UserId, string Token);
}
