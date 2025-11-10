using DigiTekShop.SharedKernel.Guards;

namespace DigiTekShop.Identity.Models;

public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; } // Set by DB via SYSUTCDATETIME()

    private RolePermission() { }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        Guard.AgainstEmpty(roleId, nameof(roleId));
        Guard.AgainstEmpty(permissionId, nameof(permissionId));

        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
            // CreatedAt will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
        };
    }
}