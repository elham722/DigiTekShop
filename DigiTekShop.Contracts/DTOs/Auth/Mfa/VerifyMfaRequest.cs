using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Contracts.DTOs.Auth.Mfa;

public sealed record VerifyMfaRequest
{
    public Guid UserId { get; init; }

    public MfaMethod Method { get; init; } = MfaMethod.Totp;

    public string Code { get; init; } = default!;

    public bool TrustThisDevice { get; init; }

}