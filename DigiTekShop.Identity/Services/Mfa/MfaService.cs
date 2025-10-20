using DigiTekShop.Contracts.Abstractions.Identity.Device;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.Options.Auth;

namespace DigiTekShop.Identity.Services.Mfa;

public sealed class MfaService : IMfaService
{
    private readonly ICurrentClient _client;
    private readonly IIdentityGateway _id;
    private readonly IDeviceRegistry _devices;
    private readonly ITokenService _tokens;
    private readonly LoginFlowOptions _opts;

    public MfaService(
        ICurrentClient client,
        IIdentityGateway id,
        IDeviceRegistry devices,
        ITokenService tokens,
        IOptions<LoginFlowOptions> opts)
    {
        _client = client;
        _id = id;
        _devices = devices;
        _tokens = tokens;
        _opts = opts.Value;
    }

    public async Task<Result<LoginResponse>> VerifyAsync(VerifyMfaRequest dto, CancellationToken ct)
    {
        var user = await _id.FindByIdAsync(dto.UserId, ct);
        if (user is null)
            return Result<LoginResponse>.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

       
        var ok = dto.Method == SharedKernel.Enums.Auth.MfaMethod.Totp
            ? await _id.VerifyTotpAsync(user, dto.Code, ct)
            : await _id.VerifySecondFactorAsync(user, dto.Method, dto.Code, ct);

        if (!ok)
            return Result<LoginResponse>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        
        var deviceId = _client.DeviceId ?? "unknown";
        DateTimeOffset? trustedUntil = null;
        if (dto.TrustThisDevice && _opts.TrustDeviceDays > 0)
        {
            trustedUntil = await _devices.TrustAsync(
                user.Id, deviceId, TimeSpan.FromDays(_opts.TrustDeviceDays), ct);
        }

        var issued = await _tokens.IssueAsync(user.Id, ct);
        if (issued.IsFailure)
            return Result<LoginResponse>.Failure(issued.Errors!, issued.ErrorCode!);

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
}
