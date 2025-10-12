using DigiTekShop.SharedKernel.Utilities;

namespace DigiTekShop.Contracts.Auth.LoginAttempts;

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
