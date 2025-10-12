using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.Contracts.Abstractions.Paging;

public sealed record PagedRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    int Page = 1,

    [Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
    int Size = 10,

    string? SortBy = null,

    bool Ascending = true,

    string? SearchTerm = null
);
