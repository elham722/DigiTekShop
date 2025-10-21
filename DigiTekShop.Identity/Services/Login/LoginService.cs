using DigiTekShop.Contracts.Abstractions.Identity.Device;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Mfa;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.SharedKernel.Enums.Auth;
using DigiTekShop.SharedKernel.Utilities.Security;

namespace DigiTekShop.Identity.Services.Login;

public sealed class LoginService : ILoginService
{
    private readonly ICurrentClient _client;
    private readonly IIdentityGateway _id;
    private readonly IRateLimiter _rateLimiter;
    private readonly IDeviceRegistry _devices;
    private readonly ITokenService _tokens;
    private readonly ILoginAttemptService _attempts;
    private readonly LoginFlowOptions _opts;

    public LoginService(
        ICurrentClient client,
        IIdentityGateway id,
        IRateLimiter rateLimiter,
        IDeviceRegistry devices,
        ITokenService tokens,
        ILoginAttemptService attempts,
        IOptions<LoginFlowOptions> opts)
    {
        _client = client;
        _id = id;
        _rateLimiter = rateLimiter;
        _devices = devices;
        _tokens = tokens;
        _attempts = attempts;
        _opts = opts.Value;
    }

    public async Task<Result<LoginResultDto>> LoginAsync(LoginRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
        {
            await TryRecordAsync(userId: null, status: LoginStatus.Failed, login: dto.Login, ct);
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        var ip = _client.IpAddress ?? "n/a";
        var ipKey = Hashing.Sha256Base64Url(ip);
        var rlKey = $"login:{dto.Login}:{ipKey}";
        var win = TimeSpan.FromSeconds(_opts.RateLimit.WindowSeconds);

        var allowed = await _rateLimiter.ShouldAllowAsync(rlKey, _opts.RateLimit.Limit, win, ct);
        if (!allowed)
        {
            await TryRecordAsync(userId: null, status: LoginStatus.Failed, login: dto.Login, ct);
            return Result<LoginResultDto>.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
        }

        var user = await _id.FindByLoginAsync(dto.Login, ct);
        if (user is null)
        {
            await _id.UniformDelayAsync(ct);
            await TryRecordAsync(userId: null, status: LoginStatus.Failed, login: dto.Login, ct);
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        if (await _id.IsLockedOutAsync(user, ct))
        {
            await TryRecordAsync(user.Id, LoginStatus.LockedOut, dto.Login, ct);
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        var passOk = await _id.CheckPasswordAsync(user, dto.Password!, ct);
        if (!passOk)
        {
            await _id.AccessFailedAsync(user, ct);
            await TryRecordAsync(user.Id, LoginStatus.Failed, dto.Login, ct);
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        if (!_id.CanSignIn(user))
        {
            await TryRecordAsync(user.Id, LoginStatus.Failed, dto.Login, ct);
            return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        var deviceId = _client.DeviceId ?? "unknown";
        await _devices.UpsertAsync(user.Id, deviceId, _client.UserAgent, _client.IpAddress, ct);

        
        var mfaRequiredForUser = _opts.EnableMfa && await _id.IsMfaRequiredAsync(user, ct);
        var deviceTrusted = await _devices.IsTrustedAsync(user.Id, deviceId, ct);
        var needsMfa = mfaRequiredForUser && !deviceTrusted;

        if (needsMfa && string.IsNullOrWhiteSpace(dto.TotpCode))
        {
            await TryRecordAsync(user.Id, LoginStatus.RequiresMfa, dto.Login, ct);
            var challenge = new LoginMfaChallengeResponse
            {
                UserId = user.Id,
                RequireMfa = true,
                Methods = await _id.GetAvailableMfaMethodsAsync(user, ct),
                DeviceTrusted = false,
                ChallengeTtlSeconds = _opts.MfaChallengeTtlSeconds
            };
            return new LoginResultDto { Challenge = challenge };
        }

        if (needsMfa)
        {
            var ok = await _id.VerifyTotpAsync(user, dto.TotpCode!, ct);
            if (!ok)
            {
                await TryRecordAsync(user.Id, LoginStatus.Failed, dto.Login, ct);
                return Result<LoginResultDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
            }
        }


        DateTimeOffset? trustedUntil = null;
        if (dto.RememberMe && needsMfa && _opts.TrustDeviceDays > 0)
        {
            trustedUntil = await _devices.TrustAsync(
                user.Id, deviceId, TimeSpan.FromDays(_opts.TrustDeviceDays), ct);
        }

        
        var issued = await _tokens.IssueAsync(user.Id, ct);
        if (issued.IsFailure)
        {
            await TryRecordAsync(user.Id, LoginStatus.Failed, dto.Login, ct);
            return Result<LoginResultDto>.Failure(issued.Errors!, issued.ErrorCode!);
        }
        await TryRecordAsync(user.Id, LoginStatus.Success, dto.Login, ct);
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

    private async Task TryRecordAsync(Guid? userId, LoginStatus status, string? login = null, CancellationToken ct = default)
    {
        try
        {
            await _attempts.RecordLoginAttemptAsync(userId, status, ipAddress: null, userAgent: null, loginNameOrEmail: login, ct);
        }
        catch { }
    }

    #endregion
}
