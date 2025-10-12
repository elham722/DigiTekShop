namespace DigiTekShop.Contracts.Auth.Lockout
{
    public record LockUserRequestDto(Guid UserId, DateTimeOffset? LockoutEnd);
}
