using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Identity.Services;

public class PermissionEvaluatorService : IPermissionEvaluatorService
{
    private readonly UserManager<User> _userManager;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly ILogger<PermissionEvaluatorService> _logger;

    public PermissionEvaluatorService(
        UserManager<User> userManager,
        DigiTekShopIdentityDbContext context,
        ILogger<PermissionEvaluatorService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(permissionName))
            return false;

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return false;

            
            var directPermission = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == user.Id && 
                           up.Permission.Name == permissionName && 
                           up.Permission.IsActive)
                .FirstOrDefaultAsync(ct);

            if (directPermission != null)
                return directPermission.IsGranted;

            
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
                    return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {PermissionName} for user {UserId}", permissionName, userId);
            return false;
        }
    }

    public async Task<Result<IEnumerable<string>>> GetEffectivePermissionsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<string>>.Failure("User ID is required");

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
                return Result<IEnumerable<string>>.Failure("User not found or inactive");

            var effectivePermissions = new HashSet<string>();

            
            var rolePermissions = await GetRolePermissionsAsync(userId, ct);
            if (rolePermissions.IsSuccess)
            {
                foreach (var permission in rolePermissions.Value)
                {
                    effectivePermissions.Add(permission);
                }
            }

            
            var directPermissions = await GetDirectPermissionsAsync(userId, ct);
            if (directPermissions.IsSuccess)
            {
                foreach (var directPermission in directPermissions.Value)
                {
                    var userPermission = await _context.UserPermissions
                        .Include(up => up.Permission)
                        .Where(up => up.UserId == user.Id && 
                                   up.Permission.Name == directPermission && 
                                   up.Permission.IsActive)
                        .FirstOrDefaultAsync(ct);

                    if (userPermission != null)
                    {
                        if (userPermission.IsGranted)
                        {
                            effectivePermissions.Add(directPermission);
                        }
                        else
                        {
                            effectivePermissions.Remove(directPermission);
                        }
                    }
                }
            }

            return Result<IEnumerable<string>>.Success(effectivePermissions);
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
}
