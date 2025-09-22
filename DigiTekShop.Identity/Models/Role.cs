using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Models
{
    public class Role:IdentityRole
    {
        public ICollection<RolePermission> Permissions { get; private set; } = new List<RolePermission>();
    }
}
