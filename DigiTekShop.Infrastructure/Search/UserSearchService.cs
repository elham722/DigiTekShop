using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.DTOs.Search;
using DigiTekShop.Contracts.Options.Search;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Result = DigiTekShop.SharedKernel.Results.Result;

namespace DigiTekShop.Infrastructure.Search;

public sealed class UserSearchService : IUserSearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<UserSearchService> _logger;

    public UserSearchService(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options,
        ILogger<UserSearchService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<UserSearchResult>> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            var from = (page - 1) * pageSize;
            var hasQuery = !string.IsNullOrWhiteSpace(query);

            SearchRequestDescriptor<UserSearchDocument> searchDescriptor = new SearchRequestDescriptor<UserSearchDocument>()
                .Indices(_options.UsersIndex)
                .From(from)
                .Size(pageSize);

            if (hasQuery)
            {
                searchDescriptor = searchDescriptor.Query(q => q.Bool(b => b
                    .Should(sh => sh
                        .MultiMatch(mm => mm
                            .Query(query)
                            .Fields(new[] { "fullName^2", "email", "phone" })
                            .Fuzziness(new Fuzziness("AUTO"))
                            .Type(TextQueryType.BestFields)
                        ),
                        sh => sh
                        .MatchPhrase(mp => mp
                            .Field("fullName")
                            .Query(query)
                            .Boost(3.0f)
                        )
                    )
                    .MinimumShouldMatch(1)
                    .Filter(f => f
                        .Term(t => t.Field("isDeleted").Value(false))
                    )
                ));
            }
            else
            {
                searchDescriptor = searchDescriptor.Query(q => q.Bool(b => b
                    .Filter(f => f
                        .Term(t => t.Field("isDeleted").Value(false))
                    )
                ));
            }

            searchDescriptor = searchDescriptor.Sort(sort => sort
                .Score(sc => sc.Order(SortOrder.Desc))
                .Field("createdAtUtc", f => f.Order(SortOrder.Desc))
            );

            var searchResponse = await _client.SearchAsync<UserSearchDocument>(searchDescriptor, ct);

            if (!searchResponse.IsValidResponse)
            {
                _logger.LogError("Search failed: {Error}", searchResponse.DebugInformation);
                return Result<UserSearchResult>.Failure(ErrorCodes.Search.SEARCH_FAILED);
            }

            var items = searchResponse.Documents.ToList();
            
            // در Elasticsearch client v9، Total یک long است
            var totalCount = (int)searchResponse.Total;

            var result = new UserSearchResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Result<UserSearchResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during search: {Query}", query);
            return Result<UserSearchResult>.Failure(ErrorCodes.Search.SEARCH_FAILED);
        }
    }

    public async Task<Result> IndexUserAsync(UserSearchDocument document, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.IndexAsync(document, _options.UsersIndex, d => d.Id(document.Id), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to index user {UserId}: {Error}", document.Id, response.DebugInformation);
                return Result.Failure(ErrorCodes.Search.INDEXING_FAILED);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while indexing user {UserId}", document.Id);
            return Result.Failure(ErrorCodes.Search.INDEXING_FAILED);
        }
    }

    public async Task<Result> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.DeleteAsync(_options.UsersIndex, userId, ct);

            if (!response.IsValidResponse && response.Result != Elastic.Clients.Elasticsearch.Result.NotFound)
            {
                _logger.LogError("Failed to delete user {UserId} from index: {Error}", userId, response.DebugInformation);
                return Result.Failure(ErrorCodes.Search.INDEXING_FAILED);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting user {UserId} from index", userId);
            return Result.Failure(ErrorCodes.Search.INDEXING_FAILED);
        }
    }

    public async Task<Result> UpdateUserAsync(UserSearchDocument document, CancellationToken ct = default)
    {
        // در Elasticsearch، update همان index است (upsert)
        return await IndexUserAsync(document, ct);
    }
}

