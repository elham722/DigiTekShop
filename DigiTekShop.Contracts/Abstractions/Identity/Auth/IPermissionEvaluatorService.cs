namespace DigiTekShop.Contracts.Abstractions.Identity.Auth;


public interface IPermissionEvaluatorService
{
    Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default);
    Task<Result<IEnumerable<string>>> GetEffectivePermissionsAsync(string userId, CancellationToken ct = default);
    Task<Result<Dictionary<string, bool>>> CheckMultiplePermissionsAsync(string userId, IEnumerable<string> permissionNames, CancellationToken ct = default);
    Task<Result<IEnumerable<string>>> GetRolePermissionsAsync(string userId, CancellationToken ct = default);
    Task<Result<IEnumerable<string>>> GetDirectPermissionsAsync(string userId, CancellationToken ct = default);

    void InvalidateUserPermissionCache(string userId);
    void InvalidateUserPermissionCache(string userId, string permissionName);
    Task InvalidateRolePermissionCacheAsync(string roleName, CancellationToken ct = default);
    void ClearAllPermissionCache();
}
