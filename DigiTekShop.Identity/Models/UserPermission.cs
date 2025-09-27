using System;
using DigiTekShop.SharedKernel.Guards;

namespace DigiTekShop.Identity.Models
{
    public class UserPermission
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid UserId { get; private set; }
        public User User { get; private set; } = default!;
        
        public Guid PermissionId { get; private set; }
        public Permission Permission { get; private set; } = default!;
        
        public bool IsGranted { get; private set; } = true; 

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
            };
        }

        public void Grant() => IsGranted = true;

        public void Deny() => IsGranted = false;

        public void Toggle() => IsGranted = !IsGranted;
    }
}