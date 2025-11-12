namespace DigiTekShop.MVC.Models;
public sealed record SetAuthCookieRequest
{
    public required string AccessToken { get; init; }
    public string? ReturnUrl { get; init; }
    public bool IsNewUser { get; init; }
}
