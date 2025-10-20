using DigiTekShop.Contracts.DTOs.Auth.SecurityEvent;
using DigiTekShop.Contracts.Options.Security;
using DigiTekShop.SharedKernel.Enums.Security;
using System.Text.Json;
using DigiTekShop.SharedKernel.Utilities.Security;

namespace DigiTekShop.Identity.Services.Security;

public sealed class SecurityEventService : ISecurityEventService
{
    private static class Events
    {
        public static readonly EventId Record = new(52001, nameof(RecordSecurityEventAsync));
        public static readonly EventId Query = new(52002, nameof(GetUnresolvedEventsAsync));
        public static readonly EventId Stats = new(52003, nameof(GetSecurityEventStatsAsync));
        public static readonly EventId Resolve = new(52004, nameof(ResolveSecurityEventAsync));
        public static readonly EventId Cleanup = new(52005, nameof(CleanupOldEventsAsync));
    }

    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IDateTimeProvider _time;
    private readonly ILogger<SecurityEventService> _log;
    private readonly SecurityEventsOptions _opts;

    public SecurityEventService(
        DigiTekShopIdentityDbContext db,
        IDateTimeProvider time,
        IOptions<SecurityEventsOptions> opts,
        ILogger<SecurityEventService> log)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _opts = opts?.Value ?? new SecurityEventsOptions();
    }

    public async Task<Result<SecurityEventDto>> RecordSecurityEventAsync(
        SecurityEventCreateDto request, CancellationToken ct = default)
    {
        try
        {
            var ip = SafeTake(request.IpAddress, _opts.MaxIpLength);
            var ua = SafeTake(request.UserAgent, _opts.MaxUserAgentLength);
            var did = SafeTake(request.DeviceId, _opts.MaxDeviceIdLength);
            var meta = SafeTake(request.MetadataJson, _opts.MaxMetadataLength);

           
            var entity = SecurityEvent.Create(
                type: request.EventType,
                userId: request.UserId,
                ipAddress: ip,
                userAgent: ua,
                deviceId: did,
                metadataJson: meta);

            _db.SecurityEvents.Add(entity);
            await _db.SaveChangesAsync(ct);

            _log.LogWarning(Events.Record,
                "Security event recorded. type={Type}, uid={UserId}, ip={Ip}",
                request.EventType, request.UserId, SensitiveDataMasker.MaskIpAddress(ip));

            return Result<SecurityEventDto>.Success(ToDto(entity));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Record, ex, "Record security event failed. type={Type} uid={UserId}",
                request.EventType, request.UserId);
            return Result<SecurityEventDto>.Failure("Failed to record security event");
        }
    }

    public Task<Result<SecurityEventDto>> RecordSecurityEventAsync<T>(
        SecurityEventType type,
        T metadata,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        CancellationToken ct = default)
    {
        var meta = metadata is null ? null : JsonSerializer.Serialize(metadata);
        var create = new SecurityEventCreateDto(type, userId, ipAddress, userAgent, deviceId, meta);
        return RecordSecurityEventAsync(create, ct);
    }

   
    public async Task<Result<IEnumerable<SecurityEventDto>>> GetUnresolvedEventsAsync(
        int limit = 100, CancellationToken ct = default)
    {
        try
        {
            var take = NormalizeLimit(limit, _opts.MaxListLimit);

            var list = await _db.SecurityEvents.AsNoTracking()
                .Where(se => !se.IsResolved)
                .OrderByDescending(se => se.OccurredAt)
                .Take(take)
                .Select(se => new SecurityEventDto(
                    se.Id,
                    se.Type,
                    se.UserId,
                    se.IpAddress,
                    se.UserAgent,
                    se.DeviceId,
                    se.MetadataJson,
                    se.OccurredAt,
                    se.IsResolved,
                    se.ResolvedAt,
                    se.ResolvedBy,
                    se.ResolutionNotes,
                   
                    se.Type == SecurityEventType.SystemIntrusion
                        || se.Type == SecurityEventType.DataBreach
                        || se.Type == SecurityEventType.BruteForceAttempt
                        || se.Type == SecurityEventType.TokenReplay
                        || se.Type == SecurityEventType.DeviceSuspicious
                        || se.Type == SecurityEventType.UnauthorizedAccess
                        ? "High"
                        : (se.Type == SecurityEventType.LoginFailed
                            || se.Type == SecurityEventType.AccountLocked
                            || se.Type == SecurityEventType.MfaFailed
                            || se.Type == SecurityEventType.RefreshTokenAnomaly
                            || se.Type == SecurityEventType.DeviceUntrusted
                            || se.Type == SecurityEventType.PermissionDenied
                            || se.Type == SecurityEventType.RateLimitExceeded
                            ? "Medium"
                            : "Low")
                ))
                .ToListAsync(ct);

            return Result<IEnumerable<SecurityEventDto>>.Success(list);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Query, ex, "GetUnresolvedEvents failed");
            return Result<IEnumerable<SecurityEventDto>>.Failure("Failed to get unresolved events");
        }
    }

    public async Task<Result<IEnumerable<SecurityEventDto>>> GetUserSecurityEventsAsync(
        Guid userId, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            if (userId == Guid.Empty)
                return Result<IEnumerable<SecurityEventDto>>.Success(Array.Empty<SecurityEventDto>());

            var take = NormalizeLimit(limit, _opts.MaxListLimit);

            var list = await _db.SecurityEvents.AsNoTracking()
                .Where(se => se.UserId == userId)
                .OrderByDescending(se => se.OccurredAt)
                .Take(take)
                .Select(se => new SecurityEventDto(
                    se.Id,
                    se.Type,
                    se.UserId,
                    se.IpAddress,
                    se.UserAgent,
                    se.DeviceId,
                    se.MetadataJson,
                    se.OccurredAt,
                    se.IsResolved,
                    se.ResolvedAt,
                    se.ResolvedBy,
                    se.ResolutionNotes,
                    se.Type == SecurityEventType.SystemIntrusion
                        || se.Type == SecurityEventType.DataBreach
                        || se.Type == SecurityEventType.BruteForceAttempt
                        || se.Type == SecurityEventType.TokenReplay
                        || se.Type == SecurityEventType.DeviceSuspicious
                        || se.Type == SecurityEventType.UnauthorizedAccess
                        ? "High"
                        : (se.Type == SecurityEventType.LoginFailed
                            || se.Type == SecurityEventType.AccountLocked
                            || se.Type == SecurityEventType.MfaFailed
                            || se.Type == SecurityEventType.RefreshTokenAnomaly
                            || se.Type == SecurityEventType.DeviceUntrusted
                            || se.Type == SecurityEventType.PermissionDenied
                            || se.Type == SecurityEventType.RateLimitExceeded
                            ? "Medium"
                            : "Low")
                ))
                .ToListAsync(ct);

            return Result<IEnumerable<SecurityEventDto>>.Success(list);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Query, ex, "GetUserSecurityEvents failed. userId={UserId}", userId);
            return Result<IEnumerable<SecurityEventDto>>.Failure("Failed to get user security events");
        }
    }

    public async Task<Result<IEnumerable<SecurityEventDto>>> GetSecurityEventsFromIpAsync(
        string ipAddress, TimeSpan timeWindow, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result<IEnumerable<SecurityEventDto>>.Failure("IP address is required");

        try
        {
            var cutoff = _time.UtcNow - timeWindow;

            var list = await _db.SecurityEvents.AsNoTracking()
                .Where(se => se.IpAddress == ipAddress && se.OccurredAt >= cutoff)
                .OrderByDescending(se => se.OccurredAt)
                .Select(se => new SecurityEventDto(
                    se.Id,
                    se.Type,
                    se.UserId,
                    se.IpAddress,
                    se.UserAgent,
                    se.DeviceId,
                    se.MetadataJson,
                    se.OccurredAt,
                    se.IsResolved,
                    se.ResolvedAt,
                    se.ResolvedBy,
                    se.ResolutionNotes,
                    se.Type == SecurityEventType.SystemIntrusion
                        || se.Type == SecurityEventType.DataBreach
                        || se.Type == SecurityEventType.BruteForceAttempt
                        || se.Type == SecurityEventType.TokenReplay
                        || se.Type == SecurityEventType.DeviceSuspicious
                        || se.Type == SecurityEventType.UnauthorizedAccess
                        ? "High"
                        : (se.Type == SecurityEventType.LoginFailed
                            || se.Type == SecurityEventType.AccountLocked
                            || se.Type == SecurityEventType.MfaFailed
                            || se.Type == SecurityEventType.RefreshTokenAnomaly
                            || se.Type == SecurityEventType.DeviceUntrusted
                            || se.Type == SecurityEventType.PermissionDenied
                            || se.Type == SecurityEventType.RateLimitExceeded
                            ? "Medium"
                            : "Low")
                ))
                .ToListAsync(ct);

            return Result<IEnumerable<SecurityEventDto>>.Success(list);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Query, ex, "GetSecurityEventsFromIp failed. ip={Ip}", ipAddress);
            return Result<IEnumerable<SecurityEventDto>>.Failure("Failed to get events from IP");
        }
    }

   
    public async Task<Result<bool>> ResolveSecurityEventAsync(
        SecurityEventResolveDto request, CancellationToken ct = default)
    {
        try
        {
            var e = await _db.SecurityEvents.FirstOrDefaultAsync(se => se.Id == request.EventId, ct);
            if (e is null) return Result<bool>.Failure("Security event not found");

            if (!e.IsResolved)
            {
                e.Resolve(request.ResolvedBy, request.ResolutionNotes);
                await _db.SaveChangesAsync(ct);
            }

            _log.LogInformation(Events.Resolve, "Security event resolved. id={Id} by={By}", request.EventId, request.ResolvedBy);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Resolve, ex, "Resolve event failed. id={Id}", request.EventId);
            return Result<bool>.Failure("Failed to resolve security event");
        }
    }

   
    public async Task<Result<SecurityEventStatsDto>> GetSecurityEventStatsAsync(
        TimeSpan timeWindow, CancellationToken ct = default)
    {
        try
        {
            var since = _time.UtcNow - timeWindow;
            var q = _db.SecurityEvents.AsNoTracking().Where(e => e.OccurredAt >= since);

            var totalTask = q.CountAsync(ct);
            var unresolvedTask = q.CountAsync(e => !e.IsResolved, ct);

            var highTask = q.CountAsync(e =>
                e.Type == SecurityEventType.SystemIntrusion
                || e.Type == SecurityEventType.DataBreach
                || e.Type == SecurityEventType.BruteForceAttempt
                || e.Type == SecurityEventType.TokenReplay
                || e.Type == SecurityEventType.DeviceSuspicious
                || e.Type == SecurityEventType.UnauthorizedAccess, ct);

            var mediumTask = q.CountAsync(e =>
                e.Type == SecurityEventType.LoginFailed
                || e.Type == SecurityEventType.AccountLocked
                || e.Type == SecurityEventType.MfaFailed
                || e.Type == SecurityEventType.RefreshTokenAnomaly
                || e.Type == SecurityEventType.DeviceUntrusted
                || e.Type == SecurityEventType.PermissionDenied
                || e.Type == SecurityEventType.RateLimitExceeded, ct);

            var byTypeTask = q.GroupBy(e => e.Type)
                .Select(g => new { g.Key, Cnt = g.Count() })
                .ToDictionaryAsync(x => x.Key.ToString(), x => x.Cnt, ct);

            var byIpTask = q.Where(e => e.IpAddress != null && e.IpAddress != "")
                .GroupBy(e => e.IpAddress!)
                .Select(g => new { Ip = g.Key, Cnt = g.Count() })
                .OrderByDescending(x => x.Cnt)
                .Take(_opts.TopIpCount)
                .ToDictionaryAsync(x => x.Ip, x => x.Cnt, ct);

            await Task.WhenAll(totalTask, unresolvedTask, highTask, mediumTask, byTypeTask, byIpTask);

            var lowCount = totalTask.Result - (highTask.Result + mediumTask.Result);

            var stats = new SecurityEventStatsDto(
                TotalEvents: totalTask.Result,
                UnresolvedEvents: unresolvedTask.Result,
                HighSeverityEvents: highTask.Result,
                MediumSeverityEvents: mediumTask.Result,
                LowSeverityEvents: lowCount,
                EventsByType: byTypeTask.Result,
                EventsByIp: byIpTask.Result
            );

            return stats;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Stats, ex, "GetSecurityEventStats failed");
            return Result<SecurityEventStatsDto>.Failure("Failed to get security event stats");
        }
    }

    public async Task<Result<int>> CleanupOldEventsAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        try
        {
            if (olderThan <= TimeSpan.Zero) return 0;
            var cutoff = _time.UtcNow - olderThan;

            var deleted = await _db.SecurityEvents
                .Where(se => se.OccurredAt < cutoff && se.IsResolved)
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
                _log.LogInformation(Events.Cleanup, "Security events cleanup: {Count}", deleted);

            return deleted;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Cleanup, ex, "CleanupOldEvents failed");
            return Result<int>.Failure("Failed to cleanup old events");
        }
    }

    
    private static SecurityEventDto ToDto(SecurityEvent e) => new(
        e.Id,
        e.Type,
        e.UserId,
        e.IpAddress,
        e.UserAgent,
        e.DeviceId,
        e.MetadataJson,
        e.OccurredAt,
        e.IsResolved,
        e.ResolvedAt,
        e.ResolvedBy,
        e.ResolutionNotes,
        e.IsHighSeverity ? "High" : (e.IsMediumSeverity ? "Medium" : "Low")
    );

    private static string? SafeTake(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);

    private static int NormalizeLimit(int requested, int maxAllowed)
        => requested <= 0 ? Math.Min(50, maxAllowed) : (requested > maxAllowed ? maxAllowed : requested);

}
