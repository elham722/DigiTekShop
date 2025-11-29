using System.Security.Claims;

namespace DigiTekShop.SharedKernel.Authorization;

/// <summary>
/// Extension methods for ClaimsPrincipal to check permissions.
/// Use these in MVC Views and Controllers to conditionally show/hide UI elements.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Checks if the user has a specific permission.
    /// Supports wildcard permissions (* and module.*)
    /// </summary>
    /// <param name="user">The claims principal (usually from HttpContext.User)</param>
    /// <param name="permission">The permission to check (e.g., Permissions.Admin.UsersView)</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    /// <example>
    /// // In Razor view:
    /// @if (User.HasPermission(Permissions.Admin.UsersLock))
    /// {
    ///     <button>Lock User</button>
    /// }
    /// </example>
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

        // 1. Exact match
        if (userPermissions.Contains(permission))
            return true;

        // 2. Full wildcard (SuperAdmin)
        if (userPermissions.Contains("*"))
            return true;

        // 3. Module wildcard (e.g., "admin.*" matches "admin.users.view")
        var dotIndex = permission.IndexOf('.');
        if (dotIndex > 0)
        {
            var modulePrefix = permission[..dotIndex];
            if (userPermissions.Contains($"{modulePrefix}.*"))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the user has any of the specified permissions.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <param name="permissions">The permissions to check (OR logic)</param>
    /// <returns>True if the user has at least one of the permissions</returns>
    /// <example>
    /// @if (User.HasAnyPermission(Permissions.Admin.UsersView, Permissions.Admin.UsersManage))
    /// {
    ///     <a href="/admin/users">Users</a>
    /// }
    /// </example>
    public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        if (permissions == null || permissions.Length == 0)
            return false;

        var userPermissions = user.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return permissions.Any(p => userPermissions.Contains(p));
    }

    /// <summary>
    /// Checks if the user has all of the specified permissions.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <param name="permissions">The permissions to check (AND logic)</param>
    /// <returns>True if the user has all of the permissions</returns>
    /// <example>
    /// @if (User.HasAllPermissions(Permissions.Admin.UsersView, Permissions.Admin.UsersLock))
    /// {
    ///     <button>Lock User</button>
    /// }
    /// </example>
    public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        if (permissions == null || permissions.Length == 0)
            return true;

        var userPermissions = user.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return permissions.All(p => userPermissions.Contains(p));
    }

    /// <summary>
    /// Gets all permissions the user has.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>List of permission strings</returns>
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

    /// <summary>
    /// Checks if the user is in a specific role.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <param name="role">The role name</param>
    /// <returns>True if the user is in the role</returns>
    public static bool HasRole(this ClaimsPrincipal user, string role)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return user.IsInRole(role);
    }

    /// <summary>
    /// Gets all roles the user has.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>List of role names</returns>
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

    /// <summary>
    /// Gets the user's ID from claims.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>User ID or null if not found</returns>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the user's email from claims.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Email or null if not found</returns>
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value;
    }
}

