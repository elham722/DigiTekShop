namespace DigiTekShop.Contracts.DTOs.Auth.Logout;
public sealed record LogoutAllRequest
{
    public string? Reason { get; init; }

    public string? UserAgent { get; init; }

    public string? Ip { get; init; }
}
