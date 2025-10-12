
using DigiTekShop.Contracts.Abstractions.Paging;

namespace DigiTekShop.Contracts.Extensions.Paging;

public static class PaginationExtensions
{
 
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, PagedRequest request)
    {
        return query
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size);
    }

   
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortBy, bool ascending = true)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        // This is a simplified version - in production, you'd want more sophisticated sorting
        // For now, we'll just return the query as-is
        // TODO: Implement dynamic sorting based on sortBy parameter
        return query;
    }

   
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, string? searchTerm, 
        Func<IQueryable<T>, string, IQueryable<T>> searchExpression)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return searchExpression(query, searchTerm);
    }
}
