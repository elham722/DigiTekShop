namespace DigiTekShop.API.Models;

public sealed record ApiResponse<T>(
    T Data,
    object? Meta = null,
    string? TraceId = null,
    DateTimeOffset? Timestamp = null
);