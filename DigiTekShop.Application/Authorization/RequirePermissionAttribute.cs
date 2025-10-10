using Microsoft.AspNetCore.Authorization;

namespace DigiTekShop.Application.Authorization;

/// <summary>
/// Authorization attribute that requires a specific permission
/// </summary>
/// <example>
/// [RequirePermission("Products.View")]
/// public async Task&lt;IActionResult&gt; GetProducts() { ... }
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission) : base()
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or empty", nameof(permission));

        // Set policy name following the format: "Permission:{PermissionName}"
        Policy = $"Permission:{permission}";
    }

    /// <summary>
    /// The permission name required for authorization
    /// </summary>
    public string Permission => Policy?.Substring("Permission:".Length) ?? string.Empty;
}

