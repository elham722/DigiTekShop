namespace DigiTekShop.Contracts.DTOs.Auth.Logout;
public sealed record LogoutAllRequest
{
    public string? Reason { get; init; }

}
