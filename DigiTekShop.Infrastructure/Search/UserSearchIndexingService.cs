using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.Options.Search;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Result = DigiTekShop.SharedKernel.Results.Result;

namespace DigiTekShop.Infrastructure.Search;

public sealed class UserSearchIndexingService : IUserSearchIndexingService
{
    private readonly IUserDataProvider _userDataProvider;
    private readonly IUserSearchService _userSearchService;
    private readonly IElasticsearchIndexManager _indexManager;
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<UserSearchIndexingService> _logger;

    public UserSearchIndexingService(
        IUserDataProvider userDataProvider,
        IUserSearchService userSearchService,
        IElasticsearchIndexManager indexManager,
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        ILogger<UserSearchIndexingService> logger)
    {
        _userDataProvider = userDataProvider;
        _userSearchService = userSearchService;
        _indexManager = indexManager;
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result> ReindexAllUsersAsync(CancellationToken ct = default)
    {
        const int batchSize = 500;
        
        try
        {
            // اطمینان از وجود ایندکس
            var indexExists = await _indexManager.IndexExistsAsync(_options.UsersIndex, ct);
            if (!indexExists)
            {
                _logger.LogInformation("Index {IndexName} does not exist, creating it...", _options.UsersIndex);
                var createResult = await _indexManager.CreateIndexAsync(_options.UsersIndex, ct);
                if (!createResult.IsSuccess)
                {
                    _logger.LogError("Failed to create index {IndexName}", _options.UsersIndex);
                    return createResult;
                }
            }

            var total = await _userDataProvider.GetTotalCountAsync(ct);
            var processed = 0;

            _logger.LogInformation("Starting user reindex. Total users: {Total}", total);

            while (true)
            {
                var docs = await _userDataProvider.GetUsersBatchAsync(processed, batchSize, ct);

                if (docs.Count == 0)
                    break;

                // استفاده از Bulk API برای بهینه‌سازی
                var bulkResponse = await _client.BulkAsync(b => b
                    .Index(_options.UsersIndex)
                    .IndexMany(docs, (descriptor, doc) => descriptor.Id(doc.Id)),
                    ct);

                if (!bulkResponse.IsValidResponse)
                {
                    _logger.LogError("Bulk indexing failed at batch {Processed}/{Total}: {Error}",
                        processed, total, bulkResponse.DebugInformation);
                    return Result.Failure(ErrorCodes.Search.INDEXING_FAILED);
                }

                processed += docs.Count;
                _logger.LogInformation("Indexed {Processed}/{Total} users", processed, total);
            }

            _logger.LogInformation("User reindex completed successfully. Total indexed: {Total}", processed);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during reindex operation");
            return Result.Failure(ErrorCodes.Search.INDEXING_FAILED);
        }
    }

    public async Task<Result> IndexUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var doc = await _userDataProvider.GetUserByIdAsync(userId, ct);
            if (doc is null)
            {
                _logger.LogWarning("User {UserId} not found for indexing", userId);
                return Result.Failure(ErrorCodes.Identity.USER_NOT_FOUND);
            }

            return await _userSearchService.IndexUserAsync(doc, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while indexing user {UserId}", userId);
            return Result.Failure(ErrorCodes.Search.INDEXING_FAILED);
        }
    }
}

