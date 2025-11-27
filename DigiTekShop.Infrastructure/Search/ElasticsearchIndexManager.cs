using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.Options.Search;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Result = DigiTekShop.SharedKernel.Results.Result;

namespace DigiTekShop.Infrastructure.Search;

public sealed class ElasticsearchIndexManager : IElasticsearchIndexManager
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ElasticsearchIndexManager> _logger;

    public ElasticsearchIndexManager(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticsearchIndexManager> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.Indices.ExistsAsync(indexName, ct);
            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if index {IndexName} exists", indexName);
            return false;
        }
    }

    public async Task<Result> CreateIndexAsync(string indexName, CancellationToken ct = default)
    {
        try
        {
            var exists = await IndexExistsAsync(indexName, ct);
            if (exists)
            {
                _logger.LogWarning("Index {IndexName} already exists", indexName);
                return Result.Success();
            }

            // ایجاد ایندکس با mapping ساده - Elasticsearch به صورت خودکار mapping را تشخیص می‌دهد
            var response = await _client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                ),
                ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
                return Result.Failure(ErrorCodes.Search.INDEX_CREATION_FAILED);
            }

            _logger.LogInformation("Index {IndexName} created successfully", indexName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating index {IndexName}", indexName);
            return Result.Failure(ErrorCodes.Search.INDEX_CREATION_FAILED);
        }
    }

    public async Task<Result> DeleteIndexAsync(string indexName, CancellationToken ct = default)
    {
        try
        {
            var exists = await IndexExistsAsync(indexName, ct);
            if (!exists)
            {
                _logger.LogWarning("Index {IndexName} does not exist", indexName);
                return Result.Success();
            }

            var response = await _client.Indices.DeleteAsync(indexName, ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to delete index {IndexName}: {Error}", indexName, response.DebugInformation);
                return Result.Failure(ErrorCodes.Search.INDEX_DELETION_FAILED);
            }

            _logger.LogInformation("Index {IndexName} deleted successfully", indexName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting index {IndexName}", indexName);
            return Result.Failure(ErrorCodes.Search.INDEX_DELETION_FAILED);
        }
    }

    public async Task<Result> RecreateIndexAsync(string indexName, CancellationToken ct = default)
    {
        var deleteResult = await DeleteIndexAsync(indexName, ct);
        if (!deleteResult.IsSuccess)
            return deleteResult;

        // کمی صبر کنیم تا Elasticsearch ایندکس را حذف کند
        await Task.Delay(500, ct);

        return await CreateIndexAsync(indexName, ct);
    }
}

