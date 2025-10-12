using DigiTekShop.Contracts.Auth.LoginAttempts;
using DigiTekShop.Contracts.Enums.Auth;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Identity.Services;



public class LoginAttemptService : ILoginAttemptService
{
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly ILogger<LoginAttemptService> _logger;

    private static readonly Func<DigiTekShopIdentityDbContext, string, DateTime, Task<int>> FailedFromIpSince =
        EF.CompileAsyncQuery((DigiTekShopIdentityDbContext db, string ip, DateTime cutoff) =>
            db.LoginAttempts.Count(la =>
                la.IpAddress == ip &&
                la.Status == LoginStatus.Failed &&
                la.AttemptedAt >= cutoff));

    public LoginAttemptService(
        DigiTekShopIdentityDbContext context,
        ILogger<LoginAttemptService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var attempt = LoginAttempt.Create(
                userId: userId,
                status: status,
                ipAddress: ipAddress,
                userAgent: userAgent,
                loginNameOrEmail: loginNameOrEmail);

            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync(ct);

            using (_logger.BeginScope(new { Ip = ipAddress, User = userId }))
            {
                _logger.LogInformation("Login attempt recorded: user={User}, status={Status}, ip={Ip}",
                    userId, status, MaskIp(ipAddress));
            }

            var dto = new LoginAttemptDto
            {
                Id = attempt.Id,
                UserId = attempt.UserId,
                Status = attempt.Status,
                IpAddress = attempt.IpAddress,
                UserAgent = attempt.UserAgent,
                LoginNameOrEmail = attempt.LoginNameOrEmail,
                AttemptedAt = attempt.AttemptedAt
            };
            return Result<LoginAttemptDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login attempt for user {UserId}", userId);
            return Result<LoginAttemptDto>.Failure("Failed to record login attempt");
        }
    }

    public async Task<Result<IEnumerable<LoginAttemptDto>>> GetUserLoginAttemptsAsync(
        Guid userId, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var attempts = await _context.LoginAttempts
                .AsNoTracking()
                .Where(la => la.UserId == userId)
                .OrderByDescending(la => la.AttemptedAt)
                .Take(limit)
                .Select(la => new LoginAttemptDto
                {
                    Id = la.Id,
                    UserId = la.UserId,
                    Status = la.Status,
                    IpAddress = la.IpAddress,
                    UserAgent = la.UserAgent,
                    LoginNameOrEmail = la.LoginNameOrEmail,
                    AttemptedAt = la.AttemptedAt
                })
                .ToListAsync(ct);

            return Result<IEnumerable<LoginAttemptDto>>.Success(attempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login attempts for user {UserId}", userId);
            return Result<IEnumerable<LoginAttemptDto>>.Failure("Failed to get login attempts");
        }
    }
    public async Task<Result<IEnumerable<LoginAttemptDto>>> GetLoginAttemptsByLoginNameAsync(
        string loginNameOrEmail, int limit = 50, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(loginNameOrEmail))
            return Result<IEnumerable<LoginAttemptDto>>.Failure("Login name or email is required");

        var norm = Normalize(loginNameOrEmail);

        try
        {
            var attempts = await _context.LoginAttempts
                .AsNoTracking()
                .Where(la => la.LoginNameOrEmailNormalized == norm)
                .OrderByDescending(la => la.AttemptedAt)
                .Take(limit)
                .Select(la => new LoginAttemptDto
                {
                    Id = la.Id,
                    UserId = la.UserId,
                    Status = la.Status,
                    IpAddress = la.IpAddress,
                    UserAgent = la.UserAgent,
                    LoginNameOrEmail = la.LoginNameOrEmail,
                    AttemptedAt = la.AttemptedAt
                })
                .ToListAsync(ct);

            return Result<IEnumerable<LoginAttemptDto>>.Success(attempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login attempts for login {Login}", MaskEmail(loginNameOrEmail));
            return Result<IEnumerable<LoginAttemptDto>>.Failure("Failed to get login attempts");
        }
    }
    public async Task<Result<int>> GetFailedAttemptsFromIpAsync(
        string ipAddress, TimeSpan timeWindow, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Result<int>.Failure("IP address is required");

        try
        {
            var cutoff = DateTime.UtcNow - timeWindow;

            // EF7 compiled query؛ اگر EF6 هست، معادل CountAsync معمولی را بگذارید:
            var count = await FailedFromIpSince(_context, ipAddress, cutoff);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed attempts from IP {Ip}", MaskIp(ipAddress));
            return Result<int>.Failure("Failed to get failed attempts count");
        }
    }

    public async Task<Result<int>> CleanupOldAttemptsAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow - olderThan;

            var deleted = await _context.LoginAttempts
                .Where(la => la.AttemptedAt < cutoff)
                .ExecuteDeleteAsync(ct);

            _logger.LogInformation("Cleaned up {Count} old login attempts", deleted);
            return Result<int>.Success(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old login attempts");
            return Result<int>.Failure("Failed to cleanup old attempts");
        }
    }


    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();

    private static string? MaskIp(string? ip)
        => string.IsNullOrWhiteSpace(ip) ? ip : ip.Replace(".", ".*.");

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return email;
        var parts = email.Split('@');
        var left = parts[0];
        var maskedLeft = left.Length <= 1 ? "*" : $"{left[0]}***";
        return $"{maskedLeft}@{parts[1]}";
    }
}
