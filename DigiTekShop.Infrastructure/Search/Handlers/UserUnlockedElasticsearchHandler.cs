using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Search.Handlers;

/// <summary>
/// Handler برای به‌روزرسانی Elasticsearch وقتی کاربر unlock می‌شود
/// </summary>
public sealed class UserUnlockedElasticsearchHandler : IIntegrationEventHandler<UserUnlockedIntegrationEvent>
{
    private readonly IUserSearchService _userSearchService;
    private readonly IUserDataProvider _userDataProvider;
    private readonly ILogger<UserUnlockedElasticsearchHandler> _logger;

    public UserUnlockedElasticsearchHandler(
        IUserSearchService userSearchService,
        IUserDataProvider userDataProvider,
        ILogger<UserUnlockedElasticsearchHandler> logger)
    {
        _userSearchService = userSearchService;
        _userDataProvider = userDataProvider;
        _logger = logger;
    }

    public async Task HandleAsync(UserUnlockedIntegrationEvent evt, CancellationToken ct)
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
                _logger.LogError("Failed to update unlocked user {UserId} in Elasticsearch: {Error}", 
                    evt.UserId, result.ErrorCode);
                return;
            }

            _logger.LogInformation("✅ Updated unlocked user {UserId} in Elasticsearch (Corr={CorrelationId})", 
                evt.UserId, evt.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while updating unlocked user {UserId} in Elasticsearch", evt.UserId);
        }
    }
}

