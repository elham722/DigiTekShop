using DigiTekShop.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DigiTekShop.Application.Authorization;

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
      
        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("Authorization denied: User is not authenticated for permission {Permission}", 
                requirement.Permission);
            
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                  ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? "unknown";

        
        var userPermissions = GetUserPermissions(context.User);

        
        var hasPermission = HasPermission(userPermissions, requirement.Permission);

        if (hasPermission)
        {
            _logger.LogDebug("Authorization succeeded: User {UserId} has permission {Permission} (from JWT claims)", 
                userId, requirement.Permission);
            context.Succeed(requirement);
        }
        else
        {
            
            _logger.LogDebug("Authorization denied: User {UserId} lacks permission {Permission}", 
                userId, requirement.Permission);
            
        }

        return Task.CompletedTask;
    }

    
    private static HashSet<string> GetUserPermissions(ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

  
    
    private static bool HasPermission(HashSet<string> userPermissions, string requiredPermission)
    {
        if (userPermissions.Contains(requiredPermission))
            return true;

        if (userPermissions.Contains("*"))
            return true;

     
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

file static class JwtRegisteredClaimNames
{
    public const string Sub = "sub";
}

