namespace DigiTekShop.Contracts.DTOs.Auth.Lockout
{
    public record LockoutStatusResponseDto(bool IsLockedOut, DateTimeOffset? LockoutEnd);
}
