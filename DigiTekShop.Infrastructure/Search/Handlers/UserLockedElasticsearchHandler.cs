using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Search.Handlers;

public sealed class UserLockedElasticsearchHandler : IIntegrationEventHandler<UserLockedIntegrationEvent>
{
    private readonly IUserSearchService _userSearchService;
    private readonly IUserDataProvider _userDataProvider;
    private readonly ILogger<UserLockedElasticsearchHandler> _logger;

    public UserLockedElasticsearchHandler(
        IUserSearchService userSearchService,
        IUserDataProvider userDataProvider,
        ILogger<UserLockedElasticsearchHandler> logger)
    {
        _userSearchService = userSearchService;
        _userDataProvider = userDataProvider;
        _logger = logger;
    }

    public async Task HandleAsync(UserLockedIntegrationEvent evt, CancellationToken ct)
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
                _logger.LogError("Failed to update locked user {UserId} in Elasticsearch: {Error}", 
                    evt.UserId, result.ErrorCode);
                return;
            }

            _logger.LogInformation("✅ Updated locked user {UserId} in Elasticsearch (Corr={CorrelationId})", 
                evt.UserId, evt.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while updating locked user {UserId} in Elasticsearch", evt.UserId);
        }
    }
}

