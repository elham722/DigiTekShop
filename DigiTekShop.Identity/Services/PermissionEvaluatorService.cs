using DigiTekShop.Contracts.Abstractions.Identity.Permission;
using DigiTekShop.SharedKernel.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Identity.Services;


public class PermissionEvaluatorService : IPermissionEvaluatorService
{
    private readonly UserManager<User> _userManager;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PermissionEvaluatorService> _logger;

    // Cache settings
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
    private const string CacheKeyPrefix = "perm:";

    public PermissionEvaluatorService(
        UserManager<User> userManager,
        DigiTekShopIdentityDbContext context,
        IMemoryCache memoryCache,
        ILogger<PermissionEvaluatorService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(permissionName))
            return false;

        // Try get from cache
        var cacheKey = GetPermissionCacheKey(userId, permissionName);
        if (_memoryCache.TryGetValue<bool>(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("Permission cache hit: {UserId} - {Permission}", userId, permissionName);
            return cachedResult;
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return false;

            // ✅ Priority 1 & 2: Check UserPermission (Explicit Grant or Deny)
            var directPermission = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == user.Id && 
                           up.Permission.Name == permissionName && 
                           up.Permission.IsActive)
                .FirstOrDefaultAsync(ct);

            if (directPermission != null)
            {
                // ✅ EXPLICIT: If Deny (IsGranted=false) → Deny
                // ✅ EXPLICIT: If Grant (IsGranted=true) → Grant
                var result = directPermission.IsGranted;
                CachePermissionResult(cacheKey, result);
                
                _logger.LogDebug("Permission {Permission} for user {UserId}: {Result} (UserPermission)", 
                    permissionName, userId, result ? "GRANTED" : "DENIED");
                
                return result;
            }

            // ✅ Priority 3: Check RolePermissions (if no explicit UserPermission)
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Any())
            {
                var rolePermissions = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Include(rp => rp.Role)
                    .Where(rp => userRoles.Contains(rp.Role.Name) && 
                               rp.Permission.Name == permissionName && 
                               rp.Permission.IsActive)
                    .AnyAsync(ct);

                if (rolePermissions)
                {
                    CachePermissionResult(cacheKey, true);
                    _logger.LogDebug("Permission {Permission} for user {UserId}: GRANTED (RolePermission)", 
                        permissionName, userId);
                    return true;
                }
            }

            // ✅ No permission found → Deny
            CachePermissionResult(cacheKey, false);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {PermissionName} for user {UserId}", permissionName, userId);
            return false;
        }
    }

    /// <summary>
    /// Gets all effective permissions for a user (with caching)
    /// </summary>
    /// <remarks>
    /// Algorithm:
    /// 1. Start with all RolePermissions
    /// 2. Apply UserPermissions:
    ///    - If IsGranted=true → Add to set (Grant)
    ///    - If IsGranted=false → Remove from set (Deny - highest priority)
    /// This ensures: Deny > Grant > Role
    /// </remarks>
    public async Task<Result<IEnumerable<string>>> GetEffectivePermissionsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<string>>.Failure("User ID is required");

        // Try get from cache
        var cacheKey = GetEffectivePermissionsCacheKey(userId);
        if (_memoryCache.TryGetValue<IEnumerable<string>>(cacheKey, out var cachedPermissions))
        {
            _logger.LogDebug("Effective permissions cache hit: {UserId}", userId);
            return Result<IEnumerable<string>>.Success(cachedPermissions);
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return Result<IEnumerable<string>>.Failure("User not found or inactive");

            var effectivePermissions = new HashSet<string>();

            // ✅ Step 1: Start with all RolePermissions (lowest priority)
            var rolePermissions = await GetRolePermissionsAsync(userId, ct);
            if (rolePermissions.IsSuccess)
            {
                foreach (var permission in rolePermissions.Value)
                {
                    effectivePermissions.Add(permission);
                }
            }

            // ✅ Step 2: Apply UserPermissions (Grant or Deny - highest priority)
            var userPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == user.Id && up.Permission.IsActive)
                .ToListAsync(ct);

            foreach (var userPermission in userPermissions)
            {
                if (userPermission.IsGranted)
                {
                    // ✅ Grant: Add to set (may not be from Role)
                    effectivePermissions.Add(userPermission.Permission.Name);
                }
                else
                {
                    // ✅ Deny: Remove from set (even if from Role - highest priority)
                    effectivePermissions.Remove(userPermission.Permission.Name);
                }
            }

            var result = effectivePermissions.AsEnumerable();

            // Cache the result
            _memoryCache.Set(cacheKey, result, CacheExpiration);
            _logger.LogDebug("Cached effective permissions for user {UserId}: {Count} permissions", 
                userId, effectivePermissions.Count);

            return Result<IEnumerable<string>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective permissions for user {UserId}", userId);
            return Result<IEnumerable<string>>.Failure("Failed to get effective permissions");
        }
    }

    public async Task<Result<Dictionary<string, bool>>> CheckMultiplePermissionsAsync(string userId, IEnumerable<string> permissionNames, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<Dictionary<string, bool>>.Failure("User ID is required");

        var result = new Dictionary<string, bool>();
        
        foreach (var permissionName in permissionNames)
        {
            result[permissionName] = await HasPermissionAsync(userId, permissionName, ct);
        }

        return Result<Dictionary<string, bool>>.Success(result);
    }

    public async Task<Result<IEnumerable<string>>> GetRolePermissionsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<string>>.Failure("User ID is required");

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return Result<IEnumerable<string>>.Failure("User not found or inactive");

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any())
                return Result<IEnumerable<string>>.Success(Enumerable.Empty<string>());

            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Include(rp => rp.Role)
                .Where(rp => userRoles.Contains(rp.Role.Name) && rp.Permission.IsActive)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync(ct);

            return Result<IEnumerable<string>>.Success(rolePermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions for user {UserId}", userId);
            return Result<IEnumerable<string>>.Failure("Failed to get role permissions");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetDirectPermissionsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<string>>.Failure("User ID is required");

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return Result<IEnumerable<string>>.Failure("User not found or inactive");

            var directPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == user.Id && up.Permission.IsActive)
                .Select(up => up.Permission.Name)
                .ToListAsync(ct);

            return Result<IEnumerable<string>>.Success(directPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting direct permissions for user {UserId}", userId);
            return Result<IEnumerable<string>>.Failure("Failed to get direct permissions");
        }
    }

    #region Cache Management

    /// <summary>
    /// Invalidates all permission cache for a specific user
    /// </summary>
    /// <remarks>
    /// Call this method when:
    /// - User's roles are changed
    /// - User's direct permissions are changed
    /// - Role permissions are changed (for all users with that role)
    /// </remarks>
    public void InvalidateUserPermissionCache(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        // Remove effective permissions cache
        var effectiveKey = GetEffectivePermissionsCacheKey(userId);
        _memoryCache.Remove(effectiveKey);

        _logger.LogInformation("Invalidated permission cache for user {UserId}", userId);
    }

    /// <summary>
    /// Invalidates specific permission cache for a user
    /// </summary>
    public void InvalidateUserPermissionCache(string userId, string permissionName)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(permissionName))
            return;

        // Remove specific permission cache
        var permissionKey = GetPermissionCacheKey(userId, permissionName);
        _memoryCache.Remove(permissionKey);

        // Also remove effective permissions cache
        var effectiveKey = GetEffectivePermissionsCacheKey(userId);
        _memoryCache.Remove(effectiveKey);

        _logger.LogInformation("Invalidated permission cache for user {UserId}, permission {Permission}", 
            userId, permissionName);
    }

    /// <summary>
    /// Invalidates permission cache for all users with a specific role
    /// </summary>
    /// <remarks>
    /// Call this method when role permissions are changed
    /// </remarks>
    public async Task InvalidateRolePermissionCacheAsync(string roleName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return;

        try
        {
            // Get all users with this role
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            
            foreach (var user in usersInRole)
            {
                InvalidateUserPermissionCache(user.Id.ToString());
            }

            _logger.LogInformation("Invalidated permission cache for all users in role {RoleName} ({Count} users)", 
                roleName, usersInRole.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating role permission cache for role {RoleName}", roleName);
        }
    }

    /// <summary>
    /// Clears all permission cache (use with caution!)
    /// </summary>
    public void ClearAllPermissionCache()
    {
        // Note: MemoryCache doesn't have a built-in clear all method
        // We would need to track cache keys or use a different cache implementation
        // For now, this is a placeholder
        _logger.LogWarning("ClearAllPermissionCache called - consider using Redis for better cache management");
    }

    private string GetPermissionCacheKey(string userId, string permissionName)
        => $"{CacheKeyPrefix}user:{userId}:perm:{permissionName}";

    private string GetEffectivePermissionsCacheKey(string userId)
        => $"{CacheKeyPrefix}user:{userId}:effective";

    private void CachePermissionResult(string cacheKey, bool result)
    {
        _memoryCache.Set(cacheKey, result, CacheExpiration);
    }

    #endregion
}
