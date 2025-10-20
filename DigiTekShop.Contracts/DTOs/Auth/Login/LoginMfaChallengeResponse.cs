using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Contracts.DTOs.Auth.Login;

public sealed record LoginMfaChallengeResponse
{
    public Guid UserId { get; init; }

    public bool RequireMfa { get; init; } = true;

    public IReadOnlyList<MfaMethod> Methods { get; init; } = Array.Empty<MfaMethod>();

    public bool DeviceTrusted { get; init; }

    public int? ChallengeTtlSeconds { get; init; }
}