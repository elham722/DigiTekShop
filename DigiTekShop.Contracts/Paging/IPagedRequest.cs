namespace DigiTekShop.Contracts.Paging
{
    public interface IPagedRequest
    {
        int PageNumber { get; }
        int PageSize { get; }
        IReadOnlyList<SortSpec> Sorts { get; }
        IReadOnlyList<FilterRule> Filters { get; }
    }
}
