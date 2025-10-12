namespace DigiTekShop.Contracts.DTOs.Auth.Lockout
{
    public record LockUserResponseDto(
        Guid UserId,
        bool IsLockedOut,
        DateTimeOffset? LockoutEnd,
        DateTimeOffset? PreviousLockoutEnd,
        string? Message = null
    );
}
