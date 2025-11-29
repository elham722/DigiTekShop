using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.Contracts.DTOs.Admin.Users;

public sealed class AdminUserListQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    /// <summary>
    /// Accepted values: "active", "locked" or null/empty for all.
    /// </summary>
    public string? Status { get; init; }

}

