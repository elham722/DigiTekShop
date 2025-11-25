namespace DigiTekShop.Contracts.DTOs.Admin.Users;

public sealed class AdminUserListItemDto
{
    public Guid Id { get; init; }
    public string? FullName { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsPhoneConfirmed { get; init; }
    public bool IsLocked { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

