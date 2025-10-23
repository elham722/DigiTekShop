namespace DigiTekShop.Contracts.DTOs.Auth.Me;

public sealed record MeResponse
{
    public Guid UserId { get; init; }
    public string Phone { get; init; } = default!;  
    public string? FullName { get; init; }       
    public string? Email { get; init; }          
    public bool PhoneConfirmed { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }


    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

    public string? PolicyVersion { get; init; }
}