using DigiTekShop.Contracts.Abstractions.Paging;
using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Identity.Extensions;

public static class QueryablePagingExtensions
{
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        PagedRequest request,
        CancellationToken ct = default)
    {
        var normalized = request.Normalize();

        var totalCount = await query.CountAsync(ct);

        if (totalCount == 0)
        {
            return PagedResponse<T>.Create(Array.Empty<T>(), 0, normalized);
        }

        var skip = (normalized.Page - 1) * normalized.Size;

        var items = await query
            .Skip(skip)
            .Take(normalized.Size)
            .ToListAsync(ct);

        return PagedResponse<T>.Create(items, totalCount, normalized);
    }
}
