using DigiTekShop.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DigiTekShop.Application.Authorization;

/// <summary>
/// High-performance permission authorization handler that reads permissions from JWT claims.
/// No database calls are made during authorization - all permission data is embedded in the token.
/// </summary>
/// <remarks>
/// Performance: O(1) average with HashSet for large permission sets.
/// This is much faster than DB-based authorization which requires network round-trip.
/// 
/// Supports wildcard permissions for SuperAdmin optimization:
/// - "admin.*" grants all admin.* permissions
/// - "*" grants all permissions (use with caution!)
/// </remarks>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check if user is authenticated
        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("Authorization failed: User is not authenticated for permission {Permission}", 
                requirement.Permission);
            return Task.CompletedTask; // Don't call Fail() - let other handlers try
        }

        // Get user ID for logging
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                  ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? "unknown";

        // Extract permissions once into a HashSet for O(1) lookups
        var userPermissions = GetUserPermissions(context.User);

        // Check permission (exact match or wildcard)
        var hasPermission = HasPermission(userPermissions, requirement.Permission);

        if (hasPermission)
        {
            _logger.LogDebug("Authorization succeeded: User {UserId} has permission {Permission} (from JWT claims)", 
                userId, requirement.Permission);
            context.Succeed(requirement);
        }
        else
        {
            // Use Debug instead of Warning - missing permission is a normal/expected scenario
            // Warning should be reserved for security-related issues (token reuse, suspicious activity, etc.)
            _logger.LogDebug("Authorization denied: User {UserId} lacks permission {Permission}", 
                userId, requirement.Permission);
            // Don't call context.Fail() to allow other handlers to potentially succeed
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Extracts all permission claims into a HashSet for efficient lookups.
    /// </summary>
    private static HashSet<string> GetUserPermissions(ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if user has the required permission (exact match or wildcard).
    /// Supports:
    /// - Exact match: "admin.users.view"
    /// - Module wildcard: "admin.*" (future use for SuperAdmin optimization)
    /// - Full wildcard: "*" (grants all permissions)
    /// </summary>
    private static bool HasPermission(HashSet<string> userPermissions, string requiredPermission)
    {
        // 1. Check exact match (most common case)
        if (userPermissions.Contains(requiredPermission))
            return true;

        // 2. Check full wildcard (SuperAdmin shortcut - future optimization)
        if (userPermissions.Contains("*"))
            return true;

        // 3. Check module wildcard (e.g., "admin.*" matches "admin.users.view")
        // Extract module prefix from required permission
        var dotIndex = requiredPermission.IndexOf('.');
        if (dotIndex > 0)
        {
            var modulePrefix = requiredPermission[..dotIndex];
            if (userPermissions.Contains($"{modulePrefix}.*"))
                return true;
        }

        return false;
    }
}

// Keep the JWT claim names accessible without System.IdentityModel reference
file static class JwtRegisteredClaimNames
{
    public const string Sub = "sub";
}

