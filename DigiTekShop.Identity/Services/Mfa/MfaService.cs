using DigiTekShop.Contracts.Abstractions.Identity.Device;
using DigiTekShop.Contracts.Abstractions.Identity.Mfa;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Mfa;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Identity.Services.Mfa;

public sealed class MfaService : IMfaService
{
    private readonly ICurrentClient _client;
    private readonly IIdentityGateway _id;
    private readonly IDeviceRegistry _devices;
    private readonly ITokenService _tokens;
    private readonly ILoginAttemptService _attempts;
    private readonly LoginFlowOptions _opts;

    public MfaService(
        ICurrentClient client,
        IIdentityGateway id,
        IDeviceRegistry devices,
        ITokenService tokens,
        ILoginAttemptService attempts,
        IOptions<LoginFlowOptions> opts)
    {
        _client = client;
        _id = id;
        _devices = devices;
        _tokens = tokens;
        _attempts = attempts;
        _opts = opts.Value;
    }

    public async Task<Result<LoginResponse>> VerifyAsync(VerifyMfaRequest dto, CancellationToken ct)
    {
        var user = await _id.FindByIdAsync(dto.UserId, ct);
        if (user is null)
        {
            await TryRecordAsync(null, LoginStatus.RequiresMfa, null, ct);
            return Result<LoginResponse>.Failure(ErrorCodes.Identity.USER_NOT_FOUND);
        }


        var ok = dto.Method == MfaMethod.Totp
            ? await _id.VerifyTotpAsync(user, dto.Code, ct)
            : await _id.VerifySecondFactorAsync(user, dto.Method, dto.Code, ct);

        if (!ok)
        {
            await TryRecordAsync(user.Id, LoginStatus.RequiresMfa, null, ct);
            return Result<LoginResponse>.Failure(ErrorCodes.Common.VALIDATION_FAILED);
        }


        var deviceId = _client.DeviceId ?? "unknown";
        DateTimeOffset? trustedUntil = null;
        if (dto.TrustThisDevice && _opts.TrustDeviceDays > 0)
        {
            trustedUntil = await _devices.TrustAsync(
                user.Id, deviceId, TimeSpan.FromDays(_opts.TrustDeviceDays), ct);
        }

        var issued = await _tokens.IssueAsync(user.Id, ct);
        if (issued.IsFailure)
        {
            await TryRecordAsync(user.Id, LoginStatus.Failed, null, ct);
            return Result<LoginResponse>.Failure(issued.Errors!, issued.ErrorCode!);
        }
        await TryRecordAsync(user.Id, LoginStatus.Success, null, ct);
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

        return resp; 
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
