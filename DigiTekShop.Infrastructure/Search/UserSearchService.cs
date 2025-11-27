using DigiTekShop.Contracts.Abstractions.Search;
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
        string query,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            // نرمال‌سازی page و pageSize
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var from = (page - 1) * pageSize;
            var hasQuery = !string.IsNullOrWhiteSpace(query);
            
            _logger.LogDebug("[UserSearch] Searching with query='{Query}', page={Page}, pageSize={PageSize}", 
                query, page, pageSize);

            SearchRequestDescriptor<UserSearchDocument> searchDescriptor = new SearchRequestDescriptor<UserSearchDocument>()
                .Indices(_options.UsersIndex)
                .From(from)
                .Size(pageSize);

            if (hasQuery)
            {
                // Normalize query: trim for better matching
                var normalizedQuery = query.Trim();
                
                // برای Phone: اگر query شبیه شماره تلفن است، چند فرمت مختلف را امتحان کنیم
                var phoneQueries = new List<string> { normalizedQuery };
                
                // اگر query فقط اعداد دارد و طول آن 3 یا بیشتر است، احتمالاً شماره تلفن است
                var digitsOnly = SharedKernel.Utilities.Text.Normalization.StripNonDigits(SharedKernel.Utilities.Text.Normalization.ToLatinDigits(normalizedQuery));
                if (!string.IsNullOrEmpty(digitsOnly) && digitsOnly.Length >= 3)
                {
                    // فرمت‌های مختلف شماره تلفن را اضافه کن
                    // مثال: "093" -> ["093", "+9893", "9893", "93"]
                    // مثال: "0935" -> ["0935", "+98935", "98935", "935"]
                    if (digitsOnly.StartsWith("0"))
                    {
                        // اگر با 0 شروع می‌شود: "093" یا "0935"
                        var withoutZero = digitsOnly[1..]; // "93" یا "935"
                        phoneQueries.Add($"+98{withoutZero}"); // "+9893" یا "+98935"
                        phoneQueries.Add($"98{withoutZero}"); // "9893" یا "98935"
                        phoneQueries.Add(withoutZero); // "93" یا "935" (بدون 0)
                    }
                    else if (digitsOnly.StartsWith("9") && digitsOnly.Length >= 9)
                    {
                        // اگر با 9 شروع می‌شود و حداقل 9 رقم دارد: "935403605"
                        phoneQueries.Add($"+98{digitsOnly}"); // "+98935403605"
                        phoneQueries.Add($"0{digitsOnly}"); // "0935403605"
                        phoneQueries.Add($"98{digitsOnly}"); // "98935403605"
                    }
                    else if (digitsOnly.StartsWith("98"))
                    {
                        // اگر با 98 شروع می‌شود: "9893" یا "98935"
                        phoneQueries.Add($"+{digitsOnly}"); // "+9893" یا "+98935"
                        var without98 = digitsOnly[2..]; // "93" یا "935"
                        phoneQueries.Add($"0{without98}"); // "093" یا "0935"
                        phoneQueries.Add(without98); // "93" یا "935" (بدون 98)
                    }
                    else if (digitsOnly.StartsWith("9") && digitsOnly.Length >= 2)
                    {
                        // اگر با 9 شروع می‌شود و حداقل 2 رقم دارد: "93" یا "91"
                        phoneQueries.Add($"+98{digitsOnly}"); // "+9893" یا "+9891"
                        phoneQueries.Add($"0{digitsOnly}"); // "093" یا "091"
                        phoneQueries.Add($"98{digitsOnly}"); // "9893" یا "9891"
                    }
                    
                    // آخرین 10 رقم (برای جستجوی بدون کد کشور)
                    if (digitsOnly.Length >= 10)
                    {
                        var last10 = digitsOnly[^10..];
                        phoneQueries.Add(last10); // "935403605"
                    }
                    
                    // خود digitsOnly را هم اضافه کن (برای جستجوی مستقیم)
                    phoneQueries.Add(digitsOnly);
                }
                
                // ساخت query descriptor
                var shouldQueries = new List<Action<QueryDescriptor<UserSearchDocument>>>();
                
                // 1. Match phrase on FullName (exact phrase match - highest priority)
                shouldQueries.Add(sh => sh
                    .MatchPhrase(mp => mp
                        .Field("FullName")
                        .Query(normalizedQuery)
                        .Boost(5.0f)
                    )
                );
                
                // 2. Phone queries (multiple formats with wildcard)
                // برای هر فرمت، wildcard query می‌سازیم تا partial match کار کند
                var distinctPhoneQueries = phoneQueries.Distinct().ToList();
                _logger.LogDebug("[UserSearch] Phone queries generated: {Queries}", string.Join(", ", distinctPhoneQueries));
                
                foreach (var phoneQuery in distinctPhoneQueries)
                {
                    // Wildcard برای partial match (مثلاً "0935" در "+98935403605")
                    shouldQueries.Add(sh => sh
                        .Wildcard(w => w
                            .Field("Phone")
                            .Value($"*{phoneQuery}*")
                            .CaseInsensitive(true)
                            .Boost(4.5f)
                        )
                    );
                }
                
                // همچنین Match query برای Phone
                shouldQueries.Add(sh => sh
                    .Match(m => m
                        .Field("Phone")
                        .Query(normalizedQuery)
                        .Operator(Operator.Or)
                        .Boost(3.5f)
                    )
                );
                
                // 3. Match on Email (exact or partial match)
                shouldQueries.Add(sh => sh
                    .Match(m => m
                        .Field("Email")
                        .Query(normalizedQuery)
                        .Operator(Operator.Or)
                        .Boost(4.0f)
                    )
                );
                
                // 4. Multi-match on FullName, Email, Phone (fuzzy search)
                shouldQueries.Add(sh => sh
                    .MultiMatch(mm => mm
                        .Query(normalizedQuery)
                        .Fields(new[] { "FullName^3", "Email^2", "Phone^2" })
                        .Fuzziness(new Fuzziness("AUTO"))
                        .Type(TextQueryType.BestFields)
                        .Boost(2.0f)
                    )
                );
                
                // 5. Wildcard search for partial matches in FullName
                shouldQueries.Add(sh => sh
                    .Wildcard(w => w
                        .Field("FullName")
                        .Value($"*{normalizedQuery}*")
                        .CaseInsensitive(true)
                        .Boost(1.5f)
                    )
                );
                
                // 6. Wildcard search for partial matches in Email
                shouldQueries.Add(sh => sh
                    .Wildcard(w => w
                        .Field("Email")
                        .Value($"*{normalizedQuery}*")
                        .CaseInsensitive(true)
                        .Boost(2.5f)
                    )
                );
                
                searchDescriptor = searchDescriptor.Query(q => q.Bool(b => b
                    .Should(shouldQueries.ToArray())
                    .MinimumShouldMatch(1)
                    .Filter(f => f
                        .Term(t => t.Field("IsDeleted").Value(false))
                    )
                ));
            }
            else
            {
                searchDescriptor = searchDescriptor.Query(q => q.Bool(b => b
                    .Filter(f => f
                        .Term(t => t.Field("IsDeleted").Value(false))
                    )
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

            // 👈 همین کافیه
            var totalCount = (int)searchResponse.Total;
            
            _logger.LogInformation("[UserSearch] Query='{Query}' → Found {Count} results (Total={Total})", 
                query, items.Count, totalCount);
            
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
            _logger.LogError(ex, "Exception during search: {Query}", query);
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
        // در Elasticsearch، update همان index است (upsert)
        return await IndexUserAsync(document, ct);
    }
}

