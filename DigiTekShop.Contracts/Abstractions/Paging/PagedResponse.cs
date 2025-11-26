namespace DigiTekShop.Contracts.Abstractions.Paging;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int Size,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
)
{
    public static PagedResponse<T> Create(
        IEnumerable<T> items,
        int totalCount,
        PagedRequest request)
    {
        var size = request.Size <= 0 ? 10 : request.Size;
        var totalPages = (int)Math.Ceiling((double)totalCount / size);

        return new PagedResponse<T>(
            Items: items.ToList(),
            TotalCount: totalCount,
            Page: request.Page,
            Size: size,
            TotalPages: totalPages,
            HasNext: request.Page < totalPages,
            HasPrevious: request.Page > 1
        );
    }
}
