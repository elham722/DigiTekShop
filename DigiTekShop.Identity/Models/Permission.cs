using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Models;

public class Permission
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public ICollection<RolePermission> Roles { get; private set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();

    private Permission() { }

    public static Permission Create(string name, string? description)
    {
        Guard.AgainstNullOrEmpty(name, nameof(name));

        // Normalize and truncate string fields
        var normalizedName = StringNormalizer.NormalizeAndTruncate(name, 256);
        var normalizedDescription = StringNormalizer.NormalizeAndTruncate(description, 1000);

        return new Permission()
        {
            // CreatedAt will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
            Name = normalizedName!,
            Description = normalizedDescription
        };
    }

    public void UpdateDescription(string? description)
    {
        Description = StringNormalizer.NormalizeAndTruncate(description, 1000);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public int GetRoleCount() => Roles.Count;

    public int GetDirectUserCount() => UserPermissions.Count(up => up.IsGranted);

    public bool IsInUse => Roles.Any() || UserPermissions.Any();
}
