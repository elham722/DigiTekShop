namespace DigiTekShop.Contracts.Paging
{
    public interface IPagedResult<out T>
    {
        IReadOnlyList<T> Items { get; }
        int TotalCount { get; }
        int PageNumber { get; }
        int PageSize { get; }
        int TotalPages { get; }
        bool HasNext { get; }
        bool HasPrevious { get; }
    }
}
