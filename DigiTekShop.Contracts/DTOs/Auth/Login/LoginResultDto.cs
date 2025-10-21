using DigiTekShop.Contracts.DTOs.Auth.Mfa;

namespace DigiTekShop.Contracts.DTOs.Auth.Login;

public sealed record LoginResultDto
{
    public LoginResponse? Success { get; init; }
    public LoginMfaChallengeResponse? Challenge { get; init; }

    public static LoginResultDto FromSuccess(LoginResponse r) => new() { Success = r };
    public static LoginResultDto FromChallenge(LoginMfaChallengeResponse c) => new() { Challenge = c };

    public bool IsSuccess => Success is not null;
    public bool IsChallenge => Challenge is not null;
}