using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Search.Handlers;

public sealed class UserRegisteredElasticsearchHandler
    : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    private readonly IUserDataProvider _userDataProvider;
    private readonly IUserSearchService _userSearchService;
    private readonly ILogger<UserRegisteredElasticsearchHandler> _logger;

    public UserRegisteredElasticsearchHandler(
        IUserDataProvider userDataProvider,
        IUserSearchService userSearchService,
        ILogger<UserRegisteredElasticsearchHandler> logger)
    {
        _userDataProvider = userDataProvider;
        _userSearchService = userSearchService;
        _logger = logger;
    }

    public async Task HandleAsync(UserRegisteredIntegrationEvent evt, CancellationToken ct)
    {
        try
        {
            // از DB اطلاعات کامل و نهایی کاربر را بخوان
            var userDoc = await _userDataProvider.GetUserByIdAsync(evt.UserId, ct);
            if (userDoc is null)
            {
                _logger.LogWarning(
                    "User {UserId} not found in DB while handling UserRegisteredIntegrationEvent",
                    evt.UserId);
                return;
            }

            // در Elasticsearch upsert کن
            var result = await _userSearchService.UpdateUserAsync(userDoc, ct);
            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "Failed to index newly registered user {UserId} in Elasticsearch. ErrorCode={ErrorCode}",
                    evt.UserId,
                    result.ErrorCode);
                return;
            }

            _logger.LogInformation(
                "✅ Indexed newly registered user {UserId} in Elasticsearch (Corr={CorrelationId})",
                evt.UserId,
                evt.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception while indexing newly registered user {UserId} in Elasticsearch",
                evt.UserId);
        }
    }
}
