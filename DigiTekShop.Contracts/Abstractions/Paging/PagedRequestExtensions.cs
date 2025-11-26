namespace DigiTekShop.Contracts.Abstractions.Paging;

public static class PagedRequestExtensions
{
    public static PagedRequest Normalize(this PagedRequest request, int maxPageSize = 100)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var size = request.Size <= 0 ? 10 : Math.Min(request.Size, maxPageSize);

        return request with { Page = page, Size = size };
    }
}
