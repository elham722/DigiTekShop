using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.Contracts.Options.Api;
public sealed class ApiMetadataOptions
{
    public string Title { get; init; } = "DigiTekShop API";
    public string Version { get; init; } = "v1";
    public string Description { get; init; } = "";
}

public sealed class ApiBehaviorOptions
{
    [Range(1, 1000)]
    public int MaxPageSize { get; init; } = 100;

    [Range(1, 1000)]
    public int DefaultPageSize { get; init; } = 10;
}
