using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Utilities.Text;
using Microsoft.AspNetCore.Identity;

namespace DigiTekShop.Identity.Models;

public sealed class Role : IdentityRole<Guid>
{
    public ICollection<RolePermission> Permissions { get; private set; } = new List<RolePermission>();

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Role() { }

    public static Role Create(string roleName)
    {
        Guard.AgainstNullOrEmpty(roleName, nameof(roleName));
       

        // Normalize and truncate string fields
        var normalizedName = StringNormalizer.NormalizeAndTruncate(roleName, 256);
        Guard.AgainstNullOrEmpty(normalizedName, nameof(normalizedName));

        return new Role
        {
            // CreatedAt will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
            Name = normalizedName,
            NormalizedName = normalizedName.ToUpperInvariant()
        };
    }

    public void UpdateName(string newName)
    {
        Guard.AgainstNullOrEmpty(newName, nameof(newName));

        // Normalize and truncate string fields
        var normalizedName = StringNormalizer.NormalizeAndTruncate(newName, 256);
        Guard.AgainstNullOrEmpty(normalizedName, nameof(normalizedName));

        Name = normalizedName;
        NormalizedName = normalizedName.ToUpperInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddPermission(RolePermission permission)
    {
        if (Permissions.Any(p => p.PermissionId == permission.PermissionId))
            return;

        Permissions.Add(permission);
    }

    public void RemovePermission(Guid permissionId)
    {
        var permission = Permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (permission != null)
            Permissions.Remove(permission);
    }

    public bool HasPermission(Guid permissionId)
    {
        return Permissions.Any(p => p.PermissionId == permissionId);
    }

    public int GetPermissionCount() => Permissions.Count;

    public void ClearAllPermissions()
    {
        Permissions.Clear();
    }
}