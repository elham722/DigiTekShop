using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Identity.Enums
{
    public enum AuditAction
    {
        Created,
        Updated,
        Deleted,
        Login,
        LoginFailed,
        Logout,
        PasswordChange,
        RoleAssignment,
        PermissionChange,
        TokenOperation
    }
}
