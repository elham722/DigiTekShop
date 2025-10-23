#nullable enable
using DigiTekShop.Contracts.DTOs.Auth.Me;

namespace DigiTekShop.Identity.Services.Me;

public sealed class MeService : IMeService
{
    private readonly UserManager<User> _users;
    private readonly ICurrentClient _client;
    private readonly ILogger<MeService> _logger;

    public MeService(
        UserManager<User> users,
        ICurrentClient client,
        ILogger<MeService> logger)
    {
        _users = users;
        _client = client;
        _logger = logger;
    }

    public async Task<Result<MeResponse>> GetAsync(CancellationToken ct)
    {
        var userId = _client.AccessTokenSubject;
        if (userId is null || userId == Guid.Empty)
        {
            return Result<MeResponse>.Failure(ErrorCodes.Common.UNAUTHORIZED);
        }

        var user = await _users.FindByIdAsync(userId.Value.ToString());
        if (user is null)
        {
            return Result<MeResponse>.Failure(ErrorCodes.Identity.USER_NOT_FOUND);
        }

        
        var roles = await _users.GetRolesAsync(user);
        var mfaEnabled = await _users.GetTwoFactorEnabledAsync(user);

        // var permissions = await _permissionSvc.GetUserPermissionsAsync(user.Id, ct);

       
        var resp = MapToResponse(user, roles, mfaEnabled);

        _logger.LogDebug("ME resolved for user={UserId} ip={Ip} ua={UA}",
            user.Id, _client.IpAddress ?? "n/a", _client.UserAgent ?? "n/a");

        return Result<MeResponse>.Success(resp);
    }

   
    private static MeResponse MapToResponse(User user, IEnumerable<string> roles, bool mfaEnabled)
        => new MeResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles?.ToArray() ?? Array.Empty<string>(),
        };
}
