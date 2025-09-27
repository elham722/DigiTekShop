namespace DigiTekShop.Identity.Models;
    public sealed class Role : IdentityRole<Guid>
    {
        public ICollection<RolePermission> Permissions { get; private set; } = new List<RolePermission>();

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        private Role() { }

        public static Role Create(string roleName)
        {
            Guard.AgainstNullOrEmpty(roleName,nameof(roleName));

            return new Role
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            };
        }

        public void UpdateName(string newName)
        {
            Guard.AgainstNullOrEmpty(newName, nameof(newName));

            Name = newName;
            NormalizedName = newName.ToUpperInvariant();
            UpdatedAt = DateTime.UtcNow;
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
    }