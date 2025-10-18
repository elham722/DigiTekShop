using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Contracts.DTOs.Auth.LoginAttempts;

public record LoginAttemptDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? LoginNameOrEmail { get; init; }
    public DateTime AttemptedAt { get; init; }
    public LoginStatus Status { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
