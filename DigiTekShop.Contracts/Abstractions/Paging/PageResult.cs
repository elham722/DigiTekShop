namespace DigiTekShop.Contracts.Abstractions.Paging;

public sealed class PageResult<T> : IPagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => PageNumber < TotalPages;
    public bool HasPrevious => PageNumber > 1;

    public PageResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        => (Items, TotalCount, PageNumber, PageSize) = (items, totalCount, pageNumber, pageSize);

    public static PageResult<T> Empty(int pageNumber, int pageSize)
        => new(Array.Empty<T>(), 0, pageNumber, pageSize);
}