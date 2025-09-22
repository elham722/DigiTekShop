using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Models
{
    public class Permission
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; } = default!;
        public string? Description { get; private set; }

        public ICollection<RolePermission> Roles { get; private set; } = new List<RolePermission>();
    }
}
