namespace DigiTekShop.Contracts.DTOs.Auth.Logout;
public sealed record LogoutAllRequest
{
    public Guid UserId { get; init; }
    public string? Reason { get; init; }

}
