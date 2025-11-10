using DigiTekShop.SharedKernel.Guards;

namespace DigiTekShop.Identity.Models;

public class UserPermission
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;
    
    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = default!;
    
    public bool IsGranted { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; } // Set by DB via SYSUTCDATETIME()
    public DateTimeOffset? UpdatedAt { get; private set; }

    public byte[]? RowVersion { get; private set; }

    private UserPermission() { }

    public static UserPermission Create(Guid userId, Guid permissionId, bool isGranted = true)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstEmpty(permissionId, nameof(permissionId));

        return new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            IsGranted = isGranted
            // CreatedAt will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
        };
    }

    public void Grant()
    {
        if (IsGranted) return;
        IsGranted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deny()
    {
        if (!IsGranted) return;
        IsGranted = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sets the grant status explicitly. Updates UpdatedAt only if the value changes.
    /// </summary>
    /// <returns>True if the value was changed, false if it was already set to the requested value.</returns>
    public bool SetGrant(bool isGranted)
    {
        if (IsGranted == isGranted) return false;
        IsGranted = isGranted;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public bool IsGrantedPermission => IsGranted;

    public bool IsDeniedPermission => !IsGranted;
}