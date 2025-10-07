namespace DigiTekShop.API.Models
{
    public sealed record PaginationMeta(
        int Page,
        int PageSize,
        long TotalItems,
        int TotalPages
    );
}
