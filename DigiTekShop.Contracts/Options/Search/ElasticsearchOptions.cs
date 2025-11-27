namespace DigiTekShop.Contracts.Options.Search;

public sealed class ElasticsearchOptions
{
    public string Url { get; init; } = "http://localhost:9200";
    public string UsersIndex { get; init; } = "digitek-users";
    public int RequestTimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
    public bool PrettyJson { get; init; } = false; // فقط در Development
    public bool EnableHealthCheck { get; init; } = true;
}

