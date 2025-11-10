using DigiTekShop.Contracts.DTOs.Auth.LoginAttempts;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.SharedKernel.Enums.Auth;
using DigiTekShop.SharedKernel.Utilities.Security;
using DigiTekShop.SharedKernel.Utilities.Text;
using System.Linq.Expressions;

namespace DigiTekShop.Identity.Services.Login;

public sealed class LoginAttemptService : ILoginAttemptService
{
    private static class Events
    {
        public static readonly EventId Record = new(10001, nameof(RecordLoginAttemptAsync));
        public static readonly EventId ListByUser = new(10002, nameof(GetUserLoginAttemptsAsync));
        public static readonly EventId ListByLogin = new(10003, nameof(GetLoginAttemptsByLoginNameAsync));
        public static readonly EventId FailedFromIp = new(10004, nameof(GetFailedAttemptsFromIpAsync));
        public static readonly EventId Cleanup = new(10005, nameof(CleanupOldAttemptsAsync));
    }

    private readonly DigiTekShopIdentityDbContext _db;
    private readonly ILogger<LoginAttemptService> _log;
    private readonly IDateTimeProvider _time;
    private readonly LoginAttemptOptions _opts;
    private readonly ICurrentClient _client;


    private static readonly Func<DigiTekShopIdentityDbContext, Guid, int, IAsyncEnumerable<LoginAttemptDto>> Q_UserAttempts =
        EF.CompileAsyncQuery((DigiTekShopIdentityDbContext ctx, Guid userId, int take) =>
            ctx.LoginAttempts
               .AsNoTracking()
               .Where(la => la.UserId == userId)
               .OrderByDescending(la => la.AttemptedAt)
               .Take(take)
               .Select(SelectDto()));

    private static readonly Func<DigiTekShopIdentityDbContext, string, int, IAsyncEnumerable<LoginAttemptDto>> Q_LoginAttempts =
        EF.CompileAsyncQuery((DigiTekShopIdentityDbContext ctx, string loginNorm, int take) =>
            ctx.LoginAttempts
               .AsNoTracking()
               .Where(la => la.LoginNameOrEmailNormalized == loginNorm)
               .OrderByDescending(la => la.AttemptedAt)
               .Take(take)
               .Select(SelectDto()));

    private static readonly Func<DigiTekShopIdentityDbContext, string, DateTimeOffset, Task<int>> Q_FailedFromIpSince =
        EF.CompileAsyncQuery((DigiTekShopIdentityDbContext ctx, string ip, DateTimeOffset cutoffUtc) =>
            ctx.LoginAttempts.Count(la => la.IpAddress == ip
                                       && la.Status == LoginStatus.Failed
                                       && la.AttemptedAt >= cutoffUtc));

    public LoginAttemptService(
        DigiTekShopIdentityDbContext db,
        ILogger<LoginAttemptService> log,
        IDateTimeProvider time,
        IOptions<LoginAttemptOptions> opts,
        ICurrentClient client)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _opts = opts?.Value ?? new LoginAttemptOptions();
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<Result<LoginAttemptDto>> RecordLoginAttemptAsync(
        Guid? userId,
        LoginStatus status,
        string? ipAddress = null,
        string? userAgent = null,
        string? loginNameOrEmail = null,
        CancellationToken ct = default)
    {
        try
        {

            var ip = string.IsNullOrWhiteSpace(ipAddress) ? _client.IpAddress : ipAddress;
            var ua = string.IsNullOrWhiteSpace(userAgent) ? _client.UserAgent : userAgent;
            var deviceId = _client.DeviceId;

            // CorrelationId and RequestId are optional - can be added later via ICorrelationContext if needed
            var attempt = LoginAttempt.Create(
                userId: userId,
                status: status,
                ipAddress: ip,
                userAgent: ua,
                deviceId: deviceId,
                loginNameOrEmail: loginNameOrEmail,
                correlationId: null, // Can be injected via ICorrelationContext if needed
                requestId: null);   // Can be injected via ICorrelationContext if needed

            _db.LoginAttempts.Add(attempt);
            await _db.SaveChangesAsync(ct);

            using (_log.BeginScope(new Dictionary<string, object?>
            { ["UserId"] = userId, ["Ip"] = ip, ["Status"] = status }))
            {
                if (_opts.MaskSensitiveInLogs)
                {
                    _log.LogInformation(Events.Record,
                        "Login attempt recorded: user={UserId}, status={Status}, ip={Ip}, ua={UA}",
                        userId, status,
                        SensitiveDataMasker.MaskIpAddress(ip),
                        SensitiveDataMasker.MaskUserAgent(ua, keep: 50));
                }
                else
                {
                    _log.LogInformation(Events.Record,
                        "Login attempt recorded: user={UserId}, status={Status}, ip={Ip}",
                        userId, status, ip);
                }
            }

            return ToDto(attempt);
        }
        catch (Exception ex)
        {
            _log.LogError(Events.Record, ex, "Failed to record login attempt (user={UserId})", userId);
            return Result<LoginAttemptDto>.Failure("Failed to record login attempt", "login_attempt_record_failed");
        }
    }

    public async Task<Result<IEnumerable<LoginAttemptDto>>> GetUserLoginAttemptsAsync(Guid userId, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var take = SafeLimit(limit, _opts.MaxListLimit);
            var list = new List<LoginAttemptDto>(take);
            await foreach (var dto in Q_UserAttempts(_db, userId, take).WithCancellation(ct))
                list.Add(dto);
            return list;
        }
        catch (Exception ex)
        {
            _log.LogError(Events.ListByUser, ex, "Failed to list login attempts for user {UserId}", userId);
            return Result<IEnumerable<LoginAttemptDto>>.Failure("Failed to get login attempts", "login_attempt_list_failed");
        }
    }

    public async Task<Result<IEnumerable<LoginAttemptDto>>> GetLoginAttemptsByLoginNameAsync(string loginNameOrEmail, int limit = 50, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(loginNameOrEmail))
            return Result<IEnumerable<LoginAttemptDto>>.Failure("Login name or email is required", "validation");

        try
        {
            var norm = Normalization.Normalize(loginNameOrEmail)!;
            var take = SafeLimit(limit, _opts.MaxListLimit);

            var list = new List<LoginAttemptDto>(take);
            await foreach (var dto in Q_LoginAttempts(_db, norm, take).WithCancellation(ct))
                list.Add(dto);

            return list;
        }
        catch (Exception ex)
        {
            _log.LogError(Events.ListByLogin, ex, "Failed to list login attempts for login {Login}",
                _opts.MaskSensitiveInLogs ? SensitiveDataMasker.MaskEmail(loginNameOrEmail) : loginNameOrEmail);
            return Result<IEnumerable<LoginAttemptDto>>.Failure("Failed to get login attempts", "login_attempt_list_failed");
        }
    }

    public async Task<Result<int>> GetFailedAttemptsFromIpAsync(string ipAddress, TimeSpan timeWindow, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result<int>.Failure("IP address is required", "validation");

        try
        {
            var cutoffUtc = DateTimeOffset.UtcNow - timeWindow;
            var count = await Q_FailedFromIpSince(_db, ipAddress, cutoffUtc);
            return count;
        }
        catch (Exception ex)
        {
            _log.LogError(Events.FailedFromIp, ex, "Failed to get failed attempts from IP {Ip}",
                _opts.MaskSensitiveInLogs ? SensitiveDataMasker.MaskIpAddress(ipAddress) : ipAddress);
            return Result<int>.Failure("Failed to get failed attempts count", "login_attempt_ip_failed");
        }
    }

    public async Task<Result<int>> CleanupOldAttemptsAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        try
        {
            var cutoffUtc = DateTimeOffset.UtcNow - olderThan;
            var deleted = await _db.LoginAttempts
                .Where(la => la.AttemptedAt < cutoffUtc)
                .ExecuteDeleteAsync(ct);

            _log.LogInformation(Events.Cleanup, "Cleaned {Count} old login attempts older than {CutoffUtc}", deleted, cutoffUtc);
            return deleted;
        }
        catch (Exception ex)
        {
            _log.LogError(Events.Cleanup, ex, "Failed to cleanup old login attempts");
            return Result<int>.Failure("Failed to cleanup old attempts", "login_attempt_cleanup_failed");
        }
    }

    #region Helpers

    private static int SafeLimit(int requested, int max) => requested <= 0 ? 50 : Math.Min(requested, max);

    private static Expression<Func<LoginAttempt, LoginAttemptDto>> SelectDto() => la => new LoginAttemptDto
    {
        Id = la.Id,
        UserId = la.UserId,
        Status = la.Status,
        IpAddress = la.IpAddress,
        UserAgent = la.UserAgent,
        DeviceId = la.DeviceId,
        LoginNameOrEmail = la.LoginNameOrEmail,
        AttemptedAt = la.AttemptedAt
    };

    private static Result<LoginAttemptDto> ToDto(LoginAttempt la) =>
        Result<LoginAttemptDto>.Success(new LoginAttemptDto
        {
            Id = la.Id,
            UserId = la.UserId,
            Status = la.Status,
            IpAddress = la.IpAddress,
            UserAgent = la.UserAgent,
            DeviceId = la.DeviceId,
            LoginNameOrEmail = la.LoginNameOrEmail,
            AttemptedAt = la.AttemptedAt
        });

    #endregion

}
