using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Search.Handlers;

/// <summary>
/// Handler برای به‌روزرسانی Elasticsearch وقتی اطلاعات کاربر تغییر می‌کند
/// </summary>
public sealed class UserUpdatedElasticsearchHandler : IIntegrationEventHandler<UserUpdatedIntegrationEvent>
{
    private readonly IUserSearchService _userSearchService;
    private readonly IUserDataProvider _userDataProvider;
    private readonly ILogger<UserUpdatedElasticsearchHandler> _logger;

    public UserUpdatedElasticsearchHandler(
        IUserSearchService userSearchService,
        IUserDataProvider userDataProvider,
        ILogger<UserUpdatedElasticsearchHandler> logger)
    {
        _userSearchService = userSearchService;
        _userDataProvider = userDataProvider;
        _logger = logger;
    }

    public async Task HandleAsync(UserUpdatedIntegrationEvent evt, CancellationToken ct)
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
                _logger.LogError("Failed to update user {UserId} in Elasticsearch: {Error}", 
                    evt.UserId, result.ErrorCode);
                return;
            }

            _logger.LogInformation("✅ Updated user {UserId} in Elasticsearch (Corr={CorrelationId})", 
                evt.UserId, evt.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while updating user {UserId} in Elasticsearch", evt.UserId);
        }
    }
}

