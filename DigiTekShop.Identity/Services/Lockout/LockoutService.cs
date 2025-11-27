using DigiTekShop.Contracts.DTOs.Auth.Lockout;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.Identity.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Identity.Services.Lockout;

public sealed class LockoutService : ILockoutService
{
    private static class Events
    {
        public static readonly EventId Lock = new(41001, nameof(LockUserAsync));
        public static readonly EventId Unlock = new(41002, nameof(UnlockUserAsync));
        public static readonly EventId Status = new(41003, nameof(GetLockoutStatusAsync));
        public static readonly EventId End = new(41004, nameof(GetLockoutEndTimeAsync));
    }

    private readonly UserManager<User> _users;
    private readonly IDateTimeProvider _time;
    private readonly IdentityLockoutOptions _opts;
    private readonly ILogger<LockoutService> _log;
    private readonly IDomainEventSink _sink;

    public LockoutService(
        UserManager<User> users,
        IDateTimeProvider time,
        IOptions<IdentityLockoutOptions> opts,
        ILogger<LockoutService> log,
        IDomainEventSink sink)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _opts = opts?.Value ?? new IdentityLockoutOptions();
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public async Task<Result<LockUserResponseDto>> LockUserAsync(LockUserRequestDto req, CancellationToken ct = default)
    {
        if (req.UserId == Guid.Empty)
            return Result<LockUserResponseDto>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var user = await _users.FindByIdAsync(req.UserId.ToString());
        if (user is null)
            return Result<LockUserResponseDto>.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

       
        if (!await _users.GetLockoutEnabledAsync(user))
        {
            var enable = await _users.SetLockoutEnabledAsync(user, true);
            if (!enable.Succeeded)
                return Result<LockUserResponseDto>.Failure(enable.Errors.Select(e => e.Description), ErrorCodes.Common.INTERNAL_ERROR);
        }

        var now = _time.UtcNow;
        var prevEnd = await _users.GetLockoutEndDateAsync(user);

       
        var requestedEnd = req.LockoutEnd ?? now.Add(_opts.DefaultDuration);
        var end = ClampLockoutEnd(requestedEnd, now);

        // Raise domain event BEFORE SetLockoutEndDateAsync (which calls SaveChangesAsync)
        // تا event در Outbox ذخیره شود
        _sink.Raise(new UserLockedDomainEvent(
            userId: user.Id,
            lockoutEnd: end,
            occurredOn: _time.UtcNow
        ));

        var setRes = await _users.SetLockoutEndDateAsync(user, end);
        if (!setRes.Succeeded)
            return Result<LockUserResponseDto>.Failure(setRes.Errors.Select(e => e.Description), ErrorCodes.Common.INTERNAL_ERROR);

        _log.LogWarning(Events.Lock, "User locked. userId={UserId}, until={Until:o}, prevEnd={PrevEnd:o}",
            user.Id, end, prevEnd);

        var dto = new LockUserResponseDto(user.Id, true, end, prevEnd, "User locked");
        return dto;
    }

    public async Task<Result<UnlockUserResponseDto>> UnlockUserAsync(UnlockUserRequestDto req, CancellationToken ct = default)
    {
        if (req.UserId == Guid.Empty)
            return Result<UnlockUserResponseDto>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        if (!_opts.AllowManualUnlock)
            return Result<UnlockUserResponseDto>.Failure(ErrorCodes.Common.FORBIDDEN);

        var user = await _users.FindByIdAsync(req.UserId.ToString());
        if (user is null)
            return Result<UnlockUserResponseDto>.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

        var now = _time.UtcNow;

        // Raise domain event BEFORE SetLockoutEndDateAsync (which calls SaveChangesAsync)
        // تا event در Outbox ذخیره شود
        _sink.Raise(new UserUnlockedDomainEvent(
            userId: user.Id,
            occurredOn: _time.UtcNow
        ));

        // unlock = set lockout end to now
        var setRes = await _users.SetLockoutEndDateAsync(user, now);
        if (!setRes.Succeeded)
            return Result<UnlockUserResponseDto>.Failure(setRes.Errors.Select(e => e.Description), "identity_error");

        // reset failed count
        await _users.ResetAccessFailedCountAsync(user);

        var end = await _users.GetLockoutEndDateAsync(user);

        _log.LogInformation(Events.Unlock, "User unlocked. userId={UserId}, at={Now:o}", user.Id, now);

        var dto = new UnlockUserResponseDto(user.Id, false, end, "User unlocked");
        return dto;
    }

    public async Task<Result<LockoutStatusResponseDto>> GetLockoutStatusAsync(string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid))
            return Result<LockoutStatusResponseDto>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var user = await _users.FindByIdAsync(uid.ToString());
        if (user is null)
            return Result<LockoutStatusResponseDto>.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

        var end = await _users.GetLockoutEndDateAsync(user);
        var isLocked = end.HasValue && end.Value > _time.UtcNow;

        _log.LogDebug(Events.Status, "Lockout status. userId={UserId}, locked={Locked}, end={End:o}", uid, isLocked, end);
        return new LockoutStatusResponseDto(isLocked, end);
    }

    public async Task<Result<TimeSpan?>> GetLockoutEndTimeAsync(string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid))
            return Result<TimeSpan?>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var user = await _users.FindByIdAsync(uid.ToString());
        if (user is null)
            return Result<TimeSpan?>.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

        var end = await _users.GetLockoutEndDateAsync(user);
        var now = _time.UtcNow;

        if (!end.HasValue || end.Value <= now)
            return (TimeSpan?)null;

        var remaining = end.Value - now;
        _log.LogDebug(Events.End, "Lockout remaining. userId={UserId}, remaining={Remaining}", uid, remaining);
        return remaining;
    }

    private DateTimeOffset ClampLockoutEnd(DateTimeOffset requested, DateTimeOffset now)
    {
        var minEnd = now.Add(TimeSpan.FromSeconds(5));
        var maxEnd = now.Add(_opts.MaxDuration);

        if (requested < minEnd) requested = minEnd;
        if (requested > maxEnd) requested = maxEnd;

        return requested;
    }
}
