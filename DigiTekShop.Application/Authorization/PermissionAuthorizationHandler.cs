using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DigiTekShop.Application.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionEvaluatorService _permissionEvaluator;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionEvaluatorService permissionEvaluator,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Get user ID from claims
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Authorization failed: User ID not found in claims for permission {Permission}", 
                requirement.Permission);
            context.Fail();
            return;
        }

        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Authorization failed: User {UserId} is not authenticated", userId);
            context.Fail();
            return;
        }

        try
        {
            // Evaluate permission
            var hasPermission = await _permissionEvaluator.HasPermissionAsync(userId, requirement.Permission);

            if (hasPermission)
            {
                _logger.LogDebug("Authorization succeeded: User {UserId} has permission {Permission}", 
                    userId, requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Authorization failed: User {UserId} does not have permission {Permission}", 
                    userId, requirement.Permission);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating permission {Permission} for user {UserId}", 
                requirement.Permission, userId);
            
            // Fail-safe: Deny access on error
            context.Fail();
        }
    }
}

