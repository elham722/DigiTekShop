namespace DigiTekShop.Contracts.Auth.Lockout
{
    public record UnlockUserResponseDto(
        Guid UserId,
        bool IsLockedOut,
        DateTimeOffset? LockoutEnd,
        string? Message = null
    );
}
