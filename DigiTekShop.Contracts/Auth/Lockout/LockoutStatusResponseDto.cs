namespace DigiTekShop.Contracts.Auth.Lockout
{
    public record LockoutStatusResponseDto(bool IsLockedOut, DateTimeOffset? LockoutEnd);
}
