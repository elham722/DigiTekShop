namespace DigiTekShop.Contracts.Abstractions.Paging;

public sealed record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int Size,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
)
{
    public static PagedResponse<TItem> Create<TItem>(IEnumerable<TItem> items, int totalCount, PagedRequest request)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / request.Size);

        return new PagedResponse<TItem>(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            Size: request.Size,
            TotalPages: totalPages,
            HasNext: request.Page < totalPages,
            HasPrevious: request.Page > 1
        );
    }
}