using DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;
using DigiTekShop.Contracts.DTOs.Auth.ResetPassword;
using DigiTekShop.Contracts.Options.Password;
using DigiTekShop.Identity.Helpers.EmailTemplates;
using DigiTekShop.SharedKernel.Enums.Audit;
using DigiTekShop.SharedKernel.Utilities.Text;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace DigiTekShop.Identity.Services.Password;

public sealed class PasswordResetService : IPasswordService
{
    private static class Events
    {
        public static readonly EventId Forgot = new(42001, nameof(ForgotPasswordAsync));
        public static readonly EventId Reset = new(42002, nameof(ResetPasswordAsync));
        public static readonly EventId Change = new(42003, nameof(ChangePasswordAsync));
        public static readonly EventId Cleanup = new(42004, nameof(CleanupExpiredThrottlesAsync));
        public static readonly EventId Status = new(42005, nameof(GetThrottleStatusAsync));
    }

    private readonly UserManager<User> _users;
    private readonly IEmailSender _mail;
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IPasswordHistoryService _history;
    private readonly ITokenBlacklistService? _blacklist;  
    private readonly IDateTimeProvider _time;
    private readonly PasswordResetOptions _opts;
    private readonly ILogger<PasswordResetService> _log;

    public PasswordResetService(
        UserManager<User> users,
        IEmailSender mail,
        DigiTekShopIdentityDbContext db,
        IOptions<PasswordResetOptions> opts,
        IPasswordHistoryService history,
        IDateTimeProvider time,
        ILogger<PasswordResetService> log,
        ITokenBlacklistService? blacklist = null)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _mail = mail ?? throw new ArgumentNullException(nameof(mail));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _opts = opts?.Value ?? new PasswordResetOptions();
        _blacklist = blacklist; 
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto req, CancellationToken ct = default)
    {
        try
        {
            if (!_opts.IsEnabled) return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_DISABLED);

            var emailNorm = Normalization.Normalize(req.Email);
            var user = string.IsNullOrWhiteSpace(emailNorm)
                ? null
                : await _users.FindByEmailAsync(emailNorm);

            if (user is null || user.IsDeleted || !user.EmailConfirmed)
            {
                _log.LogInformation(Events.Forgot, "Reset requested for non-existent/inactive email");
                return Result.Success(); 
            }

            if (!await CanRequestPasswordResetAsync(user.Id, ct))
                return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_COOLDOWN_ACTIVE);

            
            var identityToken = await _users.GeneratePasswordResetTokenAsync(user);

            
            await InvalidateActiveResetTokensAsync(user.Id, ct);

            var now = _time.UtcNow;
            var expiresAt = now.AddMinutes(_opts.TokenValidityMinutes);

            
            var tokenHash = HashToken(identityToken);
            var prt = PasswordResetToken.Create(
                userId: user.Id,
                tokenHash: tokenHash,
                expiresAt: expiresAt,
                ipAddress: req.IpAddress,
                userAgent: req.UserAgent);
            _db.PasswordResetTokens.Add(prt);

           
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(identityToken));
            var url = BuildResetUrl(user.Id, encoded);

            
            var content = CreateResetEmailContent(user.UserName ?? "User", url);
            var mailRes = await _mail.SendEmailAsync(user.Email!, content.Subject, content.HtmlContent, content.PlainTextContent);
            if (mailRes.IsFailure)
            {
                _db.PasswordResetTokens.Remove(prt);
                await _db.SaveChangesAsync(ct);
                return ResultFactories.Fail(ErrorCodes.Common.OPERATION_FAILED);
            }

            await _db.SaveChangesAsync(ct);

            await LogAuditAsync(user.Id, user.Email!, AuditAction.Created, "ResetLinkSent",
                $"expires={expiresAt:o}", ct);

            _log.LogInformation(Events.Forgot, "Reset email sent. user={UserId}", user.Id);
            return Result.Success();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Forgot, ex, "ForgotPassword failed");
            return ResultFactories.Fail(ErrorCodes.Common.OPERATION_FAILED);
        }
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequestDto req, CancellationToken ct = default)
    {
        try
        {
            if (!_opts.IsEnabled) return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_DISABLED);
            if (!Guid.TryParse(req.UserId, out var uid))
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_USER_FOR_PASSWORD_RESET);

            var user = await _users.FindByIdAsync(uid.ToString());
            if (user is null || user.IsDeleted)
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_USER_FOR_PASSWORD_RESET);

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(req.Token));
            }
            catch
            {
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN);
            }

            
            var tokenHash = HashToken(decoded);
            var stored = await _db.PasswordResetTokens
                .FirstOrDefaultAsync(p => p.UserId == uid && p.TokenHash == tokenHash, ct);
            if (stored is null) return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN);
            if (stored.IsExpired) return ResultFactories.Fail(ErrorCodes.Identity.TOKEN_EXPIRED);
            if (stored.IsUsed) return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN);

            if (stored.IsThrottled)
            {
                stored.RecordFailedAttempt(); 
                _db.PasswordResetTokens.Update(stored);
                await _db.SaveChangesAsync(ct);
                return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_COOLDOWN_ACTIVE);
            }

            
            var reused = await _history.ExistsInHistoryAsync(uid, req.NewPassword, maxToCheck: 5, ct);
            if (reused) return Result.Failure("New password must not match your recent passwords.");

            
            var identityRes = await _users.ResetPasswordAsync(user, decoded, req.NewPassword);
            if (!identityRes.Succeeded)
            {
                stored.RecordFailedAttempt(maxAttempts: 3, TimeSpan.FromMinutes(15));
                _db.PasswordResetTokens.Update(stored);
                await _db.SaveChangesAsync(ct);

                _log.LogWarning(Events.Reset, "Identity reset failed. user={UserId}, errors={Errors}",
                    uid, string.Join(", ", identityRes.Errors.Select(e => e.Description)));
                return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_FAILED);
            }

            
            stored.MarkAsUsed(ipAddress: req.IpAddress);
            _db.PasswordResetTokens.Update(stored);

            
            var reloaded = await _users.FindByIdAsync(user.Id.ToString());
            var newHash = reloaded?.PasswordHash ?? user.PasswordHash!;
            await _history.AddAsync(user.Id, newHash, keepLastN: 5, ct);

            
            await RevokeAllUserTokensAsync(uid, ct);

            
            if (_blacklist is not null)
                await _blacklist.RevokeAllUserTokensAsync(uid, "password_reset", ct);

            await InvalidateUserResetTokens(uid, ct);

            await _db.SaveChangesAsync(ct);

            await LogAuditAsync(uid, user.Email!, AuditAction.Updated, "PasswordResetCompleted", null, ct);
            _log.LogInformation(Events.Reset, "Password reset done. user={UserId}", uid);

            return Result.Success();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Reset, ex, "ResetPassword failed. userId={UserId}", req.UserId);
            return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_FAILED);
        }
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequestDto req, CancellationToken ct = default)
    {
        try
        {
            var user = await _users.FindByIdAsync(req.UserId.ToString());
            if (user is null || user.IsDeleted)
                return Result.Failure("User not found or inactive.");

            if (req.CurrentPassword == req.NewPassword)
                return Result.Failure("New password must be different from current password.");

            var reused = await _history.ExistsInHistoryAsync(user.Id, req.NewPassword, maxToCheck: 5, ct);
            if (reused) return Result.Failure("New password must not match your recent passwords.");

            var res = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
            if (!res.Succeeded)
                return Result.Failure(res.Errors.Select(e => e.Description));

            var reloaded = await _users.FindByIdAsync(user.Id.ToString());
            var newHash = reloaded?.PasswordHash ?? user.PasswordHash!;
            await _history.AddAsync(user.Id, newHash, keepLastN: 5, ct);

            await RevokeAllUserTokensAsync(req.UserId, ct);
            if (_blacklist is not null)
                await _blacklist.RevokeAllUserTokensAsync(req.UserId, "password_change", ct);

            _log.LogInformation(Events.Change, "Password changed. user={UserId}", req.UserId);
            return Result.Success();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Change, ex, "ChangePassword failed. userId={UserId}", req.UserId);
            return Result.Failure("Password change failed.");
        }
    }

    #region Helpers

    public async Task<bool> CanRequestPasswordResetAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_opts.AllowMultipleRequests) return true;

        var today = _time.TodayUtc.ToDateTime(TimeOnly.MinValue);
        var requestsToday = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.ActorId == userId
                     && a.TargetEntityName == "PasswordReset"
                     && a.Action == AuditAction.Created
                     && a.Timestamp >= today)
            .CountAsync(ct);

        if (requestsToday >= _opts.MaxRequestsPerDay) return false;

        var last = await GetLastPasswordResetRequestAsync(userId, ct);
        var now = _time.UtcNow;
        if (last is not null && now < last.Timestamp.AddMinutes(_opts.RequestCooldownMinutes))
            return false;

        return true;
    }

    private Task<AuditLog?> GetLastPasswordResetRequestAsync(Guid userId, CancellationToken ct)
        => _db.AuditLogs.AsNoTracking()
                        .Where(a => a.ActorId == userId
                                 && a.TargetEntityName == "PasswordReset"
                                 && a.Action == AuditAction.Created)
                        .OrderByDescending(a => a.Timestamp)
                        .FirstOrDefaultAsync(ct);

    private string BuildResetUrl(Guid userId, string encodedToken)
    {
        var baseUrl = (_opts.BaseUrl ?? string.Empty).TrimEnd('/');
        var path = (_opts.ResetPasswordPath ?? "reset-password").TrimStart('/');
        var uid = Uri.EscapeDataString(userId.ToString());
        var tok = Uri.EscapeDataString(encodedToken);
        return $"{baseUrl}/{path}?userId={uid}&token={tok}";
    }

    private PasswordResetEmailContent CreateResetEmailContent(string userName, string resetUrl)
    {
        var t = _opts.Template;
        var subject = $"Reset Your Password - {t.CompanyName}";
        var html = PasswordResetEmailTemplateHelper.CreatePasswordResetHtml(userName, resetUrl, t.CompanyName, t.SupportEmail, t.ContactUrl);
        var text = PasswordResetEmailTemplateHelper.CreatePasswordResetText(userName, resetUrl, t.CompanyName, t.SupportEmail);
        return new PasswordResetEmailContent(subject, html, text);
    }

    private async Task LogAuditAsync(Guid userId, string email, AuditAction action, string status, string? details, CancellationToken ct)
    {
        try
        {
            var desc = details is null ? status : $"{status}: {details}";
            _db.AuditLogs.Add(AuditLog.Create(userId, action, "PasswordReset", email, desc, isSuccess: true));
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Audit log failed. user={UserId}", userId);
        }
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var active = await _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
                .ToListAsync(ct);

            foreach (var t in active) t.Revoke("password reset/change: terminate all sessions");

            if (active.Count > 0) _db.RefreshTokens.UpdateRange(active);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Revoke all tokens failed. user={UserId}", userId);
        }
    }

    private async Task InvalidateActiveResetTokensAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;
            var active = await _db.PasswordResetTokens
                .Where(p => p.UserId == userId && !p.IsUsed && p.ExpiresAt > now)
                .ToListAsync(ct);

            foreach (var t in active) t.MarkAsUsed("invalidated by new request");
            if (active.Count > 0) _db.PasswordResetTokens.UpdateRange(active);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Invalidate active reset tokens failed. user={UserId}", userId);
        }
    }

    private async Task InvalidateUserResetTokens(Guid userId, CancellationToken ct)
    {
        try
        {
            var unused = await _db.PasswordResetTokens
                .Where(p => p.UserId == userId && !p.IsUsed)
                .ToListAsync(ct);

            foreach (var t in unused) t.MarkAsUsed();
            if (unused.Count > 0) _db.PasswordResetTokens.UpdateRange(unused);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Invalidate user reset tokens failed. user={UserId}", userId);
        }
    }

    public async Task<int> CleanupExpiredThrottlesAsync(CancellationToken ct = default)
    {
        try
        {
            var now = _time.UtcNow;
            var list = await _db.PasswordResetTokens
                .Where(p => p.ThrottleUntil.HasValue && p.ThrottleUntil.Value <= now)
                .ToListAsync(ct);

            foreach (var t in list) t.ClearThrottle();

            if (list.Count > 0)
            {
                _db.PasswordResetTokens.UpdateRange(list);
                await _db.SaveChangesAsync(ct);
                _log.LogInformation(Events.Cleanup, "Cleaned {Count} expired throttles", list.Count);
            }
            return list.Count;
        }
        catch (Exception ex)
        {
            _log.LogError(Events.Cleanup, ex, "Cleanup throttles failed");
            return 0;
        }
    }

    public async Task<PasswordResetThrottleStatus> GetThrottleStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var active = await _db.PasswordResetTokens
            .Where(p => p.UserId == userId && !p.IsUsed && p.ExpiresAt > now)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (active is null)
            return new PasswordResetThrottleStatus(false, false, 0, null, null);

        return new PasswordResetThrottleStatus(
            HasActiveToken: true,
            IsThrottled: active.IsThrottled,
            AttemptCount: active.AttemptCount,
            ThrottleUntil: active.ThrottleUntil,
            LastAttemptAt: active.LastAttemptAt
        );
    }

    private string HashToken(string token)
    {
        Guard.AgainstNullOrEmpty(token, nameof(token));
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    #endregion


}
