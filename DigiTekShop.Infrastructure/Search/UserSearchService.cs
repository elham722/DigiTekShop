using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Contracts.DTOs.Search;
using DigiTekShop.Contracts.Options.Search;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using DigiTekShop.SharedKernel.Utilities.Text;
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
        AdminUserSearchCriteria criteria,
        CancellationToken ct = default)
    {
        try
        {
            var page = criteria.Page <= 0 ? 1 : criteria.Page;
            var pageSize = criteria.PageSize <= 0 ? 10 : criteria.PageSize;
            var search = criteria.Search;
            var status = criteria.Status;

            var from = (page - 1) * pageSize;
            var hasQuery = !string.IsNullOrWhiteSpace(search);
            
            bool? isLockedFilter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusLower = status.Trim().ToLowerInvariant();
                if (statusLower == "active")
                    isLockedFilter = false;
                else if (statusLower == "locked")
                    isLockedFilter = true;
               
            }
            
            _logger.LogDebug("[UserSearch] Searching with query='{Query}', page={Page}, pageSize={PageSize}, status={Status}, isLockedFilter={IsLockedFilter}", 
                search, page, pageSize, status, isLockedFilter);

            SearchRequestDescriptor<UserSearchDocument> searchDescriptor = new SearchRequestDescriptor<UserSearchDocument>()
                .Indices(_options.UsersIndex)
                .From(from)
                .Size(pageSize)
                .TrackTotalHits(true); 

            if (hasQuery)
            {
                
                var normalizedQuery = search!.Trim();
                
                
                var phoneQueries = new List<string> { normalizedQuery };
                
                
                var digitsOnly = SharedKernel.Utilities.Text.Normalization.StripNonDigits(SharedKernel.Utilities.Text.Normalization.ToLatinDigits(normalizedQuery));
                if (!string.IsNullOrEmpty(digitsOnly) && digitsOnly.Length >= 3)
                {
                   
                    if (digitsOnly.StartsWith("0"))
                    {
                       
                        var withoutZero = digitsOnly[1..]; 
                        phoneQueries.Add($"+98{withoutZero}"); 
                        phoneQueries.Add($"98{withoutZero}"); 
                        phoneQueries.Add(withoutZero); 
                    }
                    else if (digitsOnly.StartsWith("9") && digitsOnly.Length >= 9)
                    {
                        
                        phoneQueries.Add($"+98{digitsOnly}"); 
                        phoneQueries.Add($"0{digitsOnly}"); 
                        phoneQueries.Add($"98{digitsOnly}"); 
                    }
                    else if (digitsOnly.StartsWith("98"))
                    {
                        phoneQueries.Add($"+{digitsOnly}"); 
                        var without98 = digitsOnly[2..]; 
                        phoneQueries.Add($"0{without98}"); 
                        phoneQueries.Add(without98); 
                    }
                    else if (digitsOnly.StartsWith("9") && digitsOnly.Length >= 2)
                    {
                        
                        phoneQueries.Add($"+98{digitsOnly}"); 
                        phoneQueries.Add($"0{digitsOnly}"); 
                        phoneQueries.Add($"98{digitsOnly}"); 
                    }
                    
                    
                    if (digitsOnly.Length >= 10)
                    {
                        var last10 = digitsOnly[^10..];
                        phoneQueries.Add(last10);
                    }
                    
       
                    phoneQueries.Add(digitsOnly);
                }
                
               
                var shouldQueries = new List<Action<QueryDescriptor<UserSearchDocument>>>();
               
                shouldQueries.Add(sh => sh
                    .MatchPhrase(mp => mp
                        .Field("FullName")
                        .Query(normalizedQuery)
                        .Boost(5.0f)
                    )
                );
                
              
                var distinctPhoneQueries = phoneQueries.Distinct().ToList();
                _logger.LogDebug("[UserSearch] Phone queries generated: {Queries}", string.Join(", ", distinctPhoneQueries));
                
                foreach (var phoneQuery in distinctPhoneQueries)
                {
                    shouldQueries.Add(sh => sh
                        .Wildcard(w => w
                            .Field("Phone")
                            .Value($"*{phoneQuery}*")
                            .CaseInsensitive(true)
                            .Boost(4.5f)
                        )
                    );
                }
                
                shouldQueries.Add(sh => sh
                    .Match(m => m
                        .Field("Phone")
                        .Query(normalizedQuery)
                        .Operator(Operator.Or)
                        .Boost(3.5f)
                    )
                );
                
                shouldQueries.Add(sh => sh
                    .Match(m => m
                        .Field("Email")
                        .Query(normalizedQuery)
                        .Operator(Operator.Or)
                        .Boost(4.0f)
                    )
                );
                
                shouldQueries.Add(sh => sh
                    .MultiMatch(mm => mm
                        .Query(normalizedQuery)
                        .Fields(new[] { "FullName^3", "Email^2", "Phone^2" })
                        .Fuzziness(new Fuzziness("AUTO"))
                        .Type(TextQueryType.BestFields)
                        .Boost(2.0f)
                    )
                );
                
                shouldQueries.Add(sh => sh
                    .Wildcard(w => w
                        .Field("FullName")
                        .Value($"*{normalizedQuery}*")
                        .CaseInsensitive(true)
                        .Boost(1.5f)
                    )
                );
                
                shouldQueries.Add(sh => sh
                    .Wildcard(w => w
                        .Field("Email")
                        .Value($"*{normalizedQuery}*")
                        .CaseInsensitive(true)
                        .Boost(2.5f)
                    )
                );
                
                var filters = new List<Action<QueryDescriptor<UserSearchDocument>>>();
                
           
                filters.Add(f => f.Term(t => t.Field("IsDeleted").Value(false)));
                
               
                if (isLockedFilter.HasValue)
                {
                    filters.Add(f => f.Term(t => t.Field("IsLocked").Value(isLockedFilter.Value)));
                }
                
                searchDescriptor = searchDescriptor.Query(q => q.Bool(b => b
                    .Should(shouldQueries.ToArray())
                    .MinimumShouldMatch(1)
                    .Filter(filters.ToArray())
                ));
            }
            else
            {
                
                var filters = new List<Action<QueryDescriptor<UserSearchDocument>>>();
                
                
                filters.Add(f => f.Term(t => t.Field("IsDeleted").Value(false)));
                
               
                if (isLockedFilter.HasValue)
                {
                    filters.Add(f => f.Term(t => t.Field("IsLocked").Value(isLockedFilter.Value)));
                }
                
                searchDescriptor = searchDescriptor.Query(q => q.Bool(b => b
                    .Filter(filters.ToArray())
                ));
            }

            searchDescriptor = searchDescriptor.Sort(sort => sort
                .Score(sc => sc.Order(SortOrder.Desc))
                .Field("CreatedAtUtc", f => f.Order(SortOrder.Desc))
            );

            var searchResponse = await _client.SearchAsync<UserSearchDocument>(searchDescriptor, ct);

            if (!searchResponse.IsValidResponse)
            {
                _logger.LogError("Search failed: {Error}", searchResponse.DebugInformation);
                return Result<UserSearchResult>.Failure(ErrorCodes.Search.SEARCH_FAILED);
            }

            var items = searchResponse.Documents.ToList();

            
            var totalCount = (int)searchResponse.Total;
            
            _logger.LogInformation("[UserSearch] Query='{Query}' → Found {Count} results (Total={Total})", 
                search, items.Count, totalCount);
            
            if (items.Count > 0)
            {
                _logger.LogDebug("[UserSearch] First result: Phone={Phone}, FullName={FullName}", 
                    items[0].Phone, items[0].FullName);
            }

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
            _logger.LogError(ex, "Exception during search: {Query}", criteria.Search);
            return Result<UserSearchResult>.Failure(ErrorCodes.Search.SEARCH_FAILED);
        }
    }

    public async Task<Result> IndexUserAsync(UserSearchDocument document, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.IndexAsync(
                document,
                document.Id,
                d => d.Index(_options.UsersIndex),
                ct);

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
       
        return await IndexUserAsync(document, ct);
    }
}

