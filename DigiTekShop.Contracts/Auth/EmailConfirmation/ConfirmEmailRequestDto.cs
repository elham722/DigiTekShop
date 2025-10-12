namespace DigiTekShop.Contracts.Auth.EmailConfirmation
{
    public record ConfirmEmailRequestDto(Guid UserId, string Token);
}
