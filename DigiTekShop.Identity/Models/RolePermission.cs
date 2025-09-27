namespace DigiTekShop.Identity.Models;
    public sealed class RolePermission
    {
        public Guid RoleId { get; private set; }
        public Role Role { get; private set; } = default!;

        public Guid PermissionId { get; private set; }
        public Permission Permission { get; private set; } = default!;

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        private RolePermission() { }

        public static RolePermission Create(Guid roleId, Guid permissionId)
        {
            Guard.AgainstEmpty(roleId, nameof(roleId));
            Guard.AgainstEmpty(permissionId, nameof(permissionId));

            return new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            };
        }
    }