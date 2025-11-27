namespace DigiTekShop.Contracts.DTOs.Admin.Users;

/// <summary>
/// Normalized search criteria for admin user search.
/// All values are already normalized (page >= 1, pageSize >= 1, etc.)
/// </summary>
public sealed record AdminUserSearchCriteria(
    string? Search,
    string? Status,
    int Page,
    int PageSize
);

