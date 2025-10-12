namespace DigiTekShop.Contracts.Abstractions.Paging
{
    public interface IPagedRequest
    {
        int PageNumber { get; }
        int PageSize { get; }
        IReadOnlyList<SortSpec> Sorts { get; }
        IReadOnlyList<FilterRule> Filters { get; }
    }
}
