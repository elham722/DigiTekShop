using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Models
{
    public class RolePermission
    {
        public string RoleId { get; private set; } = default!;
        public Role Role { get; private set; } = default!;

        public Guid PermissionId { get; private set; }
        public Permission Permission { get; private set; } = default!;
    }
}
