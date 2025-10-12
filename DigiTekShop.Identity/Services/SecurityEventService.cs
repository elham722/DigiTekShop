using DigiTekShop.Contracts.Auth.SecurityEvent;
using DigiTekShop.Contracts.Enums.Security;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigiTekShop.Identity.Services;

public class SecurityEventService : ISecurityEventService
{
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly ILogger<SecurityEventService> _logger;

    public SecurityEventService(
        DigiTekShopIdentityDbContext context,
        ILogger<SecurityEventService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Helpers

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
        null
    );

    #endregion

    #region Record

    
    public async Task<Result<SecurityEventDto>> RecordSecurityEventAsync(
        SecurityEventCreateDto request,
        CancellationToken ct = default)
    {
        try
        {
            var entity = SecurityEvent.Create(
                type: request.EventType,
                userId: request.UserId,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                deviceId: request.DeviceId,
                metadataJson: request.MetadataJson);

            _context.SecurityEvents.Add(entity);
            await _context.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Security event recorded: Type={Type}, UserId={UserId}, IP={IpAddress}",
                request.EventType, request.UserId, request.IpAddress);

            return Result<SecurityEventDto>.Success(ToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record security event Type={Type} UserId={UserId}",
                request.EventType, request.UserId);
            return Result<SecurityEventDto>.Failure("Failed to record security event");
        }
    }

   
    public async Task<Result<SecurityEventDto>> RecordSecurityEventAsync<T>(
        SecurityEventType type,
        T metadata,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        CancellationToken ct = default)
    {
        var metadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata);
        var createDto = new SecurityEventCreateDto
        {
            EventType = type,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceId = deviceId,
            MetadataJson = metadataJson
        };
        return await RecordSecurityEventAsync(createDto, ct);
    }

    #endregion

    #region Queries

    public async Task<Result<IEnumerable<SecurityEventDto>>> GetUnresolvedEventsAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        try
        {
            var list = await _context.SecurityEvents
                .Where(se => !se.IsResolved)
                .OrderByDescending(se => se.OccurredAt)
                .Take(limit)
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
                    null
                ))
                .ToListAsync(ct);

            return Result<IEnumerable<SecurityEventDto>>.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unresolved security events");
            return Result<IEnumerable<SecurityEventDto>>.Failure("Failed to get unresolved events");
        }
    }

    public async Task<Result<IEnumerable<SecurityEventDto>>> GetUserSecurityEventsAsync(
        Guid userId,
        int limit = 50,
        CancellationToken ct = default)
    {
        try
        {
            var list = await _context.SecurityEvents
                .Where(se => se.UserId == userId)
                .OrderByDescending(se => se.OccurredAt)
                .Take(limit)
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
                    null
                ))
                .ToListAsync(ct);

            return Result<IEnumerable<SecurityEventDto>>.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events for user {UserId}", userId);
            return Result<IEnumerable<SecurityEventDto>>.Failure("Failed to get user security events");
        }
    }

    public async Task<Result<IEnumerable<SecurityEventDto>>> GetSecurityEventsFromIpAsync(
        string ipAddress,
        TimeSpan timeWindow,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result<IEnumerable<SecurityEventDto>>.Failure("IP address is required");

        try
        {
            var cutoff = DateTime.UtcNow - timeWindow;

            var list = await _context.SecurityEvents
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
                    null
                ))
                .ToListAsync(ct);

            return Result<IEnumerable<SecurityEventDto>>.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events from IP {IpAddress}", ipAddress);
            return Result<IEnumerable<SecurityEventDto>>.Failure("Failed to get security events from IP");
        }
    }

    #endregion

    #region Resolve

    public async Task<Result<bool>> ResolveSecurityEventAsync(
        SecurityEventResolveDto request,
        CancellationToken ct = default)
    {
        try
        {
            var entity = await _context.SecurityEvents
                .FirstOrDefaultAsync(se => se.Id == request.EventId, ct);

            if (entity is null)
                return Result<bool>.Failure("Security event not found");

            entity.Resolve(request.ResolvedBy, request.ResolutionNotes);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Security event resolved: EventId={EventId}, ResolvedBy={ResolvedBy}",
                request.EventId, request.ResolvedBy);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve security event {EventId}", request.EventId);
            return Result<bool>.Failure("Failed to resolve security event");
        }
    }

    #endregion

    #region Stats & Cleanup

    public async Task<Result<SecurityEventStatsDto>> GetSecurityEventStatsAsync(
        TimeSpan timeWindow,
        CancellationToken ct = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow - timeWindow;

            
            var events = await _context.SecurityEvents
                .Where(se => se.OccurredAt >= cutoff)
                .ToListAsync(ct);

            var total = events.Count;
            var unresolved = events.Count(e => !e.IsResolved);
            var high = events.Count(e => e.IsHighSeverity);
            var medium = events.Count(e => e.IsMediumSeverity);
            var low = events.Count(e => e.IsLowSeverity);
            var byType = events
                .GroupBy(e => e.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            var byIp = events
                .Where(e => !string.IsNullOrWhiteSpace(e.IpAddress))
                .GroupBy(e => e.IpAddress!)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            var stats = new SecurityEventStatsDto(
                total,
                unresolved,
                high,
                medium,
                low,
                byType,
                byIp
            );

            return Result<SecurityEventStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security event stats");
            return Result<SecurityEventStatsDto>.Failure("Failed to get security event stats");
        }
    }

    public async Task<Result<int>> CleanupOldEventsAsync(
        TimeSpan olderThan,
        CancellationToken ct = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow - olderThan;

            var oldEvents = await _context.SecurityEvents
                .Where(se => se.OccurredAt < cutoff && se.IsResolved)
                .ToListAsync(ct);

            _context.SecurityEvents.RemoveRange(oldEvents);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Cleaned up {Count} old security events", oldEvents.Count);
            return Result<int>.Success(oldEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old security events");
            return Result<int>.Failure("Failed to cleanup old events");
        }
    }

    #endregion
}
