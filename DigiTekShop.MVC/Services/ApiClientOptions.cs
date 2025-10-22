namespace DigiTekShop.MVC.Services;
public sealed class ApiClientOptions
{
    public string BaseAddress { get; init; } = "https://localhost:7055";
    public int TimeoutSeconds { get; init; } = 15;
    public int RetryCount { get; init; } = 2;
    public int CircuitBreakErrors { get; init; } = 5;
    public int CircuitDurationSeconds { get; init; } = 30;
}