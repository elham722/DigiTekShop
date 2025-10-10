using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DigiTekShop.Application.Authorization;

/// <summary>
/// Custom policy provider that creates permission-based policies dynamically
/// </summary>
/// <remarks>
/// This provider creates policies on-the-fly based on permission names.
/// Policy names follow the format: "Permission:{PermissionName}"
/// Example: "Permission:Products.View" → PermissionRequirement("Products.View")
/// </remarks>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PermissionPrefix = "Permission:";
    
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if this is a permission-based policy
        if (policyName.StartsWith(PermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // Extract permission name from policy name
            var permission = policyName.Substring(PermissionPrefix.Length);

            // Create policy dynamically
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Fall back to default provider for other policies
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}

