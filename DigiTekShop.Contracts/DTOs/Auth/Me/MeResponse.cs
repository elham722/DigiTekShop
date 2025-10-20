namespace DigiTekShop.Contracts.DTOs.Auth.Me;

public sealed record MeResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public bool EmailConfirmed { get; init; }
    public string? UserName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool PhoneNumberConfirmed { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

    public string? PolicyVersion { get; init; }
}