using System.Security.Claims;

namespace DigiTekShop.SharedKernel.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        if (string.IsNullOrWhiteSpace(permission))
            return false;

        var userPermissions = user.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        
        if (userPermissions.Contains(permission))
            return true;

       
        if (userPermissions.Contains("*"))
            return true;

       
        var dotIndex = permission.IndexOf('.');
        if (dotIndex > 0)
        {
            var modulePrefix = permission[..dotIndex];
            if (userPermissions.Contains($"{modulePrefix}.*"))
                return true;
        }

        return false;
    }

  
    public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        if (permissions == null || permissions.Length == 0)
            return false;

        
        return permissions.Any(p => HasPermission(user, p));
    }


    public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        if (permissions == null || permissions.Length == 0)
            return true;

        
        return permissions.All(p => HasPermission(user, p));
    }

  
    public static IReadOnlyList<string> GetPermissions(this ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return Array.Empty<string>();

        return user.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }


    public static bool HasRole(this ClaimsPrincipal user, string role)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return user.IsInRole(role);
    }

  
    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return Array.Empty<string>();

        return user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

   
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

 
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value;
    }
}

