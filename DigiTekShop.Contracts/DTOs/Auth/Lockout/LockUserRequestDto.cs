namespace DigiTekShop.Contracts.DTOs.Auth.Lockout
{
    public record LockUserRequestDto(Guid UserId, DateTimeOffset? LockoutEnd);
}
