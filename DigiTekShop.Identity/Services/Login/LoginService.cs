using DigiTekShop.Contracts.Abstractions.Identity.Device;
using DigiTekShop.Contracts.DTOs.Auth.Login;

namespace DigiTekShop.Identity.Services.Login;

public sealed class LoginService : ILoginService
{
    private readonly ICurrentClient _client;
    private readonly IIdentityGateway _id;
    private readonly IRateLimiter _rateLimiter;
    private readonly IDeviceRegistry _devices;
    private readonly ITokenService _tokens;

    public LoginService(
        ICurrentClient client,
        IIdentityGateway id,
        IRateLimiter rateLimiter,
        IDeviceRegistry devices,
        ITokenService tokens)
    {
        _client = client;
        _id = id;
        _rateLimiter = rateLimiter;
        _devices = devices;
        _tokens = tokens;
    }

    public async Task<Result<LoginResultDto>> LoginAsync(LoginRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);

        var ip = _client.IpAddress ?? "n/a";
        var ipKey = Sha256(ip);
        var allowed = await _rateLimiter.ShouldAllowAsync($"login:{dto.Login}:{ipKey}", 8, TimeSpan.FromMinutes(1), ct);
        if (!allowed)
            return Result<LoginResultDto>.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);

        var user = await _id.FindByLoginAsync(dto.Login, ct);
        if (user is null)
        {
            await _id.UniformDelayAsync(ct);
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        if (await _id.IsLockedOutAsync(user, ct))
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);

        var passOk = await _id.CheckPasswordAsync(user, dto.Password!, ct);
        if (!passOk)
        {
            await _id.AccessFailedAsync(user, ct);
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        if (!_id.CanSignIn(user))
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);

        var deviceId = _client.DeviceId ?? "unknown";
        await _devices.UpsertAsync(user.Id, deviceId, _client.UserAgent, _client.IpAddress, ct);

        var mfaEnabled = await _id.IsMfaRequiredAsync(user, ct);
        var deviceTrust = await _devices.IsTrustedAsync(user.Id, deviceId, ct);
        var needsMfa = mfaEnabled && !deviceTrust;

        if (needsMfa && string.IsNullOrWhiteSpace(dto.TotpCode))
        {
            var challenge = new LoginMfaChallengeResponse
            {
                UserId = user.Id,
                RequireMfa = true,
                Methods = await _id.GetAvailableMfaMethodsAsync(user, ct),
                DeviceTrusted = false,
                ChallengeTtlSeconds = 120
            };
            return new LoginResultDto { Challenge = challenge }; 
        }

        if (needsMfa)
        {
            var ok = await _id.VerifyTotpAsync(user, dto.TotpCode!, ct);
            if (!ok)
                return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        DateTimeOffset? trustedUntil = null;
        if (dto.RememberMe && needsMfa)
            trustedUntil = await _devices.TrustAsync(user.Id, deviceId, TimeSpan.FromDays(30), ct);

        var issued = await _tokens.IssueAsync(user.Id, ct);
        if (issued.IsFailure)
            return Result<LoginResultDto>.Failure(issued.Errors!, issued.ErrorCode!);

        var v = issued.Value;
        var resp = new LoginResponse
        {
            AccessToken = v.AccessToken,
            RefreshToken = v.RefreshToken,
            TokenType = v.TokenType,
            ExpiresIn = v.ExpiresIn,
            IssuedAtUtc = v.IssuedAtUtc,
            ExpiresAtUtc = v.ExpiresAtUtc,
            UserId = user.Id,
            DeviceTrustedUntilUtc = trustedUntil,
            ClaimsVersion = null
        };

        return LoginResultDto.FromSuccess(resp); 
    }

    #region Helpers

    private static string Sha256(string s)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    #endregion

}
