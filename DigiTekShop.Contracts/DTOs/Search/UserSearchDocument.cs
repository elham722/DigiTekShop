namespace DigiTekShop.Contracts.DTOs.Search;
public sealed class UserSearchDocument
{
    public string Id { get; set; } = default!;
    public string? FullName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

    public bool IsLocked { get; set; }
    public bool IsPhoneConfirmed { get; set; }
    public bool IsDeleted { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public string[] Roles { get; set; } = Array.Empty<string>();
}

