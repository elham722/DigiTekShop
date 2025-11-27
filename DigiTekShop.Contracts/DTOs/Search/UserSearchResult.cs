namespace DigiTekShop.Contracts.DTOs.Search;

public sealed class UserSearchResult
{
    public IReadOnlyList<UserSearchDocument> Items { get; init; } = Array.Empty<UserSearchDocument>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

