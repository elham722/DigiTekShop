namespace DigiTekShop.Identity.Models;
    public class Permission
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = default!;
        public string? Description { get; private set; }
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        public ICollection<RolePermission> Roles { get; private set; } = new List<RolePermission>();
        public ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();

        private Permission() { }

        public static Permission Create(string name , string? description)
        {
            Guard.AgainstNullOrEmpty(name,nameof(name));
            return new Permission()
            {
                Name = name,
                Description = description
            };
        }
        public void UpdateDescription(string? description)
        {
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            UpdatedAt = DateTime.UtcNow;
        }

        public int GetRoleCount() => Roles.Count;

        public int GetDirectUserCount() => UserPermissions.Count(up => up.IsGranted);

        public bool IsInUse => Roles.Any() || UserPermissions.Any();
    }
