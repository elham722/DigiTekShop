using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Utilities.Text;
using Microsoft.AspNetCore.Identity;

namespace DigiTekShop.Identity.Models;

public sealed class Role : IdentityRole<Guid>
{
    public ICollection<RolePermission> Permissions { get; private set; } = new List<RolePermission>();

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public bool IsSystemRole { get; private set; }

    public bool IsDefaultForNewUsers { get; private set; }

    public string? Description { get; private set; }

    private Role() { }

    public static Role Create(string roleName, string? description = null, bool isSystemRole = false, bool isDefaultForNewUsers = false)
    {
        Guard.AgainstNullOrEmpty(roleName, nameof(roleName));
       

        var normalizedName = Normalization.NormalizeAndTruncate(roleName, 256);
        Guard.AgainstNullOrEmpty(normalizedName, nameof(normalizedName));

        var normalizedDescription = Normalization.NormalizeAndTruncate(description, 1000);

        return new Role
        {
            Name = normalizedName,
            NormalizedName = normalizedName.ToUpperInvariant(),
            Description = normalizedDescription,
            IsSystemRole = isSystemRole,
            IsDefaultForNewUsers = isDefaultForNewUsers
        };
    }

    public void UpdateName(string newName)
    {
        Guard.AgainstNullOrEmpty(newName, nameof(newName));

        // Normalize and truncate string fields
        var normalizedName = Normalization.NormalizeAndTruncate(newName, 256);
        Guard.AgainstNullOrEmpty(normalizedName, nameof(normalizedName));

        Name = normalizedName;
        NormalizedName = normalizedName!.ToUpperInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        Description = Normalization.NormalizeAndTruncate(description, 1000);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsSystemRole()
    {
        if (IsSystemRole) return;
        IsSystemRole = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UnmarkAsSystemRole()
    {
        if (!IsSystemRole) return;
        IsSystemRole = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDefaultForNewUsers(bool isDefault)
    {
        if (IsDefaultForNewUsers == isDefault) return;
        IsDefaultForNewUsers = isDefault;
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