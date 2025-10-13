namespace DigiTekShop.API.Security;

public sealed class ApiKeyOptions
{
    public bool Enabled { get; set; } = false;
    public string HeaderName { get; set; } = "X-API-Key";
    public string[] ValidKeys { get; set; } = Array.Empty<string>();
}