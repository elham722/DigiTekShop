
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Identity.Services.Logout;

public sealed class LogoutService : ILogoutService
{
    private readonly ITokenService _tokens;
    private readonly ICurrentClient _client;
    private readonly ILoginAttemptService _attempts;
    private readonly ILogger<LogoutService> _logger;
    private readonly ITokenBlacklistService? _blacklist; 

    public LogoutService(
        ITokenService tokens,
        ICurrentClient client,
        ILoginAttemptService attempts,
        ILogger<LogoutService> logger,
        ITokenBlacklistService? blacklist = null)
    {
        _tokens = tokens;
        _client = client;
        _attempts = attempts;
        _logger = logger;
        _blacklist = blacklist;
    }

    public async Task<Result> LogoutAsync(LogoutRequest dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        
        var currentUserId = _client.AccessTokenSubject;
        if (currentUserId is null || currentUserId != dto.UserId)
        {
            _logger.LogWarning("Security violation: User {CurrentUserId} attempted to logout user {RequestedUserId} | ip={Ip}",
                currentUserId, dto.UserId, _client.IpAddress ?? "n/a");
            return Result.Failure(ErrorCodes.Common.FORBIDDEN, "You can only logout your own account.");
        }

        var revoke = await _tokens.RevokeAsync(dto.RefreshToken!, dto.UserId, ct);
        if (revoke.IsFailure)
            return Result.Failure(revoke.Errors!, revoke.ErrorCode!);

        await BlacklistCurrentAccessTokenIfPossibleAsync("logout", ct);

        await TryRecordAsync(dto.UserId, LoginStatus.Logout, ct);

        _logger.LogInformation("User {UserId} logged out | device={DeviceId} ip={Ip}",
            dto.UserId, _client.DeviceId ?? "n/a", _client.IpAddress ?? "n/a");

        return Result.Success();
    }

    public async Task<Result> LogoutAllAsync(LogoutAllRequest dto, CancellationToken ct)
    {
        if (dto.UserId == Guid.Empty)
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

       
        var currentUserId = _client.AccessTokenSubject;
        if (currentUserId is null || currentUserId != dto.UserId)
        {
            _logger.LogWarning("Security violation: User {CurrentUserId} attempted to logout all sessions for user {RequestedUserId} | ip={Ip}",
                currentUserId, dto.UserId, _client.IpAddress ?? "n/a");
            return Result.Failure(ErrorCodes.Common.FORBIDDEN, "You can only logout your own account.");
        }

        var revoke = await _tokens.RevokeAllAsync(dto.UserId, ct);
        if (revoke.IsFailure)
            return Result.Failure(revoke.Errors!, revoke.ErrorCode!);

        if (_blacklist is not null)
        {
            try
            {
                await _blacklist.RevokeAllUserTokensAsync(dto.UserId, "logout_all", ct);
            }
            catch
            {
            }
        }

       
        await BlacklistCurrentAccessTokenIfPossibleAsync("logout_all", ct);

       
        await TryRecordAsync(dto.UserId, LoginStatus.LogoutAll, ct);

        _logger.LogInformation("User {UserId} logged out from ALL sessions | device={DeviceId} ip={Ip}",
            dto.UserId, _client.DeviceId ?? "n/a", _client.IpAddress ?? "n/a");

        return Result.Success();
    }

    #region Helpers

    private async Task BlacklistCurrentAccessTokenIfPossibleAsync(string reason, CancellationToken ct)
    {
        if (_blacklist is null) return;

        string? jti = _client.AccessTokenJti;
        DateTime? expUtc = _client.AccessTokenExpiresAtUtc;

       
        if ((string.IsNullOrWhiteSpace(jti) || expUtc is null) && !string.IsNullOrWhiteSpace(_client.AccessTokenRaw))
        {
            var info = _tokens.TryReadAccessToken(_client.AccessTokenRaw!);
            if (info.ok)
            {
                jti ??= info.jti;
                expUtc ??= info.expUtc;
            }
        }

        if (!string.IsNullOrWhiteSpace(jti) && expUtc is not null)
        {
            try
            {
                await _blacklist.RevokeAccessTokenAsync(jti!, expUtc.Value, reason, ct);
            }
            catch
            {
            }
        }
    }

    private async Task TryRecordAsync(Guid? userId, LoginStatus status, CancellationToken ct)
    {
        try
        {
            await _attempts.RecordLoginAttemptAsync(
                userId: userId,
                status: status,
                ipAddress: _client.IpAddress,
                userAgent: _client.UserAgent,
                loginNameOrEmail: null,
                ct);
        }
        catch
        {
        }
    }

    #endregion
}
