using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Search.Handlers;

/// <summary>
/// Handler برای به‌روزرسانی Elasticsearch وقتی نقش‌های کاربر تغییر می‌کند
/// </summary>
public sealed class UserRolesChangedElasticsearchHandler : IIntegrationEventHandler<UserRolesChangedIntegrationEvent>
{
    private readonly IUserSearchService _userSearchService;
    private readonly IUserDataProvider _userDataProvider;
    private readonly ILogger<UserRolesChangedElasticsearchHandler> _logger;

    public UserRolesChangedElasticsearchHandler(
        IUserSearchService userSearchService,
        IUserDataProvider userDataProvider,
        ILogger<UserRolesChangedElasticsearchHandler> logger)
    {
        _userSearchService = userSearchService;
        _userDataProvider = userDataProvider;
        _logger = logger;
    }

    public async Task HandleAsync(UserRolesChangedIntegrationEvent evt, CancellationToken ct)
    {
        try
        {
            // دریافت داده‌های کامل کاربر از DB
            var userDoc = await _userDataProvider.GetUserByIdAsync(evt.UserId, ct);
            if (userDoc is null)
            {
                _logger.LogWarning("User {UserId} not found in database, skipping Elasticsearch update", evt.UserId);
                return;
            }

            // به‌روزرسانی در Elasticsearch (upsert)
            var result = await _userSearchService.UpdateUserAsync(userDoc, ct);
            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to update user roles for {UserId} in Elasticsearch: {Error}", 
                    evt.UserId, result.ErrorCode);
                return;
            }

            _logger.LogInformation("✅ Updated user roles for {UserId} in Elasticsearch (Roles={Roles}, Corr={CorrelationId})", 
                evt.UserId, string.Join(", ", evt.Roles), evt.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while updating user roles for {UserId} in Elasticsearch", evt.UserId);
        }
    }
}

