using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth;

/// <summary>
/// Service for evaluating user permissions with caching support
/// </summary>
/// <remarks>
/// Priority Order: UserPermission.Deny > UserPermission.Grant > RolePermissions
/// </remarks>
public interface IPermissionEvaluatorService
{
    // Permission Evaluation
    Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default);
    Task<Result<IEnumerable<string>>> GetEffectivePermissionsAsync(string userId, CancellationToken ct = default);
    Task<Result<Dictionary<string, bool>>> CheckMultiplePermissionsAsync(string userId, IEnumerable<string> permissionNames, CancellationToken ct = default);
    Task<Result<IEnumerable<string>>> GetRolePermissionsAsync(string userId, CancellationToken ct = default);
    Task<Result<IEnumerable<string>>> GetDirectPermissionsAsync(string userId, CancellationToken ct = default);

    // Cache Invalidation
    /// <summary>
    /// Invalidates all permission cache for a specific user
    /// Call when user roles or permissions are changed
    /// </summary>
    void InvalidateUserPermissionCache(string userId);

    /// <summary>
    /// Invalidates specific permission cache for a user
    /// </summary>
    void InvalidateUserPermissionCache(string userId, string permissionName);

    /// <summary>
    /// Invalidates permission cache for all users with a specific role
    /// Call when role permissions are changed
    /// </summary>
    Task InvalidateRolePermissionCacheAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Clears all permission cache (use with caution!)
    /// </summary>
    void ClearAllPermissionCache();
}
