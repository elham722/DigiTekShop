namespace DigiTekShop.Contracts.Paging;

public sealed record PageRequest(
    int PageNumber = 1,
    int PageSize = 20,
    IReadOnlyList<SortSpec>? Sorts = null,
    IReadOnlyList<FilterRule>? Filters = null
) : IPagedRequest
{
    public int PageNumber { get; init; } = PageNumber < 1 ? 1 : PageNumber;
    public int PageSize { get; init; } = PageSize < 1 ? 20 : PageSize;
    public IReadOnlyList<SortSpec> Sorts { get; init; } = Sorts ?? Array.Empty<SortSpec>();
    public IReadOnlyList<FilterRule> Filters { get; init; } = Filters ?? Array.Empty<FilterRule>();
}