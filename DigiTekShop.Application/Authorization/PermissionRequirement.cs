using Microsoft.AspNetCore.Authorization;

namespace DigiTekShop.Application.Authorization;

/// <summary>
/// Authorization requirement for permission-based access control
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or empty", nameof(permission));
        
        Permission = permission;
    }
}

