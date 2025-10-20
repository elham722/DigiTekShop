using DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;
using DigiTekShop.Contracts.Abstractions.Identity.Password;
using DigiTekShop.Contracts.DTOs.Auth.ResetPassword;
using DigiTekShop.Identity.Helpers.EmailTemplates;
using DigiTekShop.SharedKernel.Enums.Audit;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;

namespace DigiTekShop.Identity.Services.Password;


public sealed class PasswordResetService : IPasswordService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly PasswordResetSettings _settings;
    private readonly IPasswordHistoryService _passwordHistory;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        UserManager<User> userManager,
        IEmailSender emailSender,
        DigiTekShopIdentityDbContext context,
        IOptions<PasswordResetSettings> settings,
        IPasswordHistoryService passwordHistory,
        ILogger<PasswordResetService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _passwordHistory = passwordHistory ?? throw new ArgumentNullException(nameof(passwordHistory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

   

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default)
    {

        return await SendResetLinkCoreAsync(request, ipAddress: request.IpAddress, userAgent: request.UserAgent, ct);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default)
    {
        try
        {

            if (!_settings.IsEnabled)
                return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_DISABLED);


            if (!Guid.TryParse(request.UserId, out var userId))
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_USER_FOR_PASSWORD_RESET);

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null || user.IsDeleted)
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_USER_FOR_PASSWORD_RESET);

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch
            {
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN);
            }

           
            var tokenHash = HashToken(decodedToken);
            var storedToken = await _context.PasswordResetTokens
                .Where(prt => prt.UserId == userId && prt.TokenHash == tokenHash)
                .FirstOrDefaultAsync(ct);

            if (storedToken == null)
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN);

            if (storedToken.IsExpired)
                return ResultFactories.Fail(ErrorCodes.Identity.TOKEN_EXPIRED);

            if (storedToken.IsUsed)
                return ResultFactories.Fail(ErrorCodes.Identity.INVALID_TOKEN); 

            if (storedToken.IsThrottled)
            {
                storedToken.RecordFailedAttempt(); 
                _context.PasswordResetTokens.Update(storedToken);
                await _context.SaveChangesAsync(ct);
                
                return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_COOLDOWN_ACTIVE);
            }


            var reused = await _passwordHistory.ExistsInHistoryAsync(
                user.Id, request.NewPassword, maxToCheck: 5, ct: ct);
            if (reused)
                return Result.Failure("New password must not match your recent passwords.");

            
            var identityRes = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            if (!identityRes.Succeeded)
            {
               
                storedToken.RecordFailedAttempt(maxAttempts: 3, TimeSpan.FromMinutes(15));
                _context.PasswordResetTokens.Update(storedToken);
                await _context.SaveChangesAsync(ct);

                var errors = identityRes.Errors.Select(e => e.Description);
                _logger.LogWarning("Password reset failed for user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_FAILED);
            }

            storedToken.MarkAsUsed(ipAddress: request.IpAddress); 
            _context.PasswordResetTokens.Update(storedToken);

            
            var reloaded = await _userManager.FindByIdAsync(user.Id.ToString());
            var newHash = reloaded?.PasswordHash ?? user.PasswordHash!;
            await _passwordHistory.AddAsync(user.Id, newHash, keepLastN: 5, ct: ct);


            await RevokeAllUserTokensAsync(userId, ct);
            await InvalidateUserResetTokens(userId, ct);

            await _context.SaveChangesAsync(ct);

            await LogAuditAsync(userId, user.Email!, AuditAction.Updated, "PasswordResetCompleted", details: null, ct);
            _logger.LogInformation("Password reset completed successfully for user {UserId}", userId);

            return Result.Success();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", request.UserId);
            return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_FAILED);
        }
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken ct = default)
    {
        try
        {

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user is null || user.IsDeleted)
                return Result.Failure("User not found or inactive.");

           
            if (request.CurrentPassword == request.NewPassword)
                return Result.Failure("New password must be different from current password.");


            var reused = await _passwordHistory.ExistsInHistoryAsync(
                user.Id, request.NewPassword, maxToCheck: 5, ct: ct);
            if (reused)
                return Result.Failure("New password must not match your recent passwords.");

           
            var res = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!res.Succeeded)
            {
                var errors = res.Errors.Select(e => e.Description);
                return Result.Failure(errors);
            }

            
            var reloaded = await _userManager.FindByIdAsync(user.Id.ToString());
            var newHash = reloaded?.PasswordHash ?? user.PasswordHash!;
            await _passwordHistory.AddAsync(user.Id, newHash, keepLastN: 5, ct: ct);


            await RevokeAllUserTokensAsync(request.UserId, ct);

            _logger.LogInformation("Password changed for user {UserId}", request.UserId);
            return Result.Success();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
            return Result.Failure("Password change failed.");
        }
    }

  
    private async Task<Result> SendResetLinkCoreAsync(ForgotPasswordRequestDto request, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        try
        {
            if (!_settings.IsEnabled)
                return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_DISABLED);

            // Validation قبلاً در ForgotPasswordAsync انجام شده است

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || user.IsDeleted || !user.EmailConfirmed)
            {
                _logger.LogWarning("Password reset requested for non-existent or inactive email: {Email}", request.Email);
                return Result.Success(); 
            }

            if (!await CanRequestPasswordResetAsync(user.Id, ct))
            {
                return ResultFactories.Fail(ErrorCodes.Identity.PASSWORD_RESET_COOLDOWN_ACTIVE);
            }

            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            await InvalidateActiveResetTokensAsync(user.Id, ct);

            var tokenHash = HashToken(identityToken);
            var tokenExpires = DateTime.UtcNow.AddMinutes(_settings.TokenValidityMinutes);

            var resetTokenEntity = PasswordResetToken.Create(user.Id, tokenHash, tokenExpires, ipAddress, userAgent);
            _context.PasswordResetTokens.Add(resetTokenEntity);

            var resetUrl = BuildResetUrl(user.Id, WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(identityToken)));

            var content = CreateResetEmailContent(user.UserName ?? "User", resetUrl);

            var emailResult = await _emailSender.SendEmailAsync(user.Email!, content.Subject, content.HtmlContent, content.PlainTextContent);
            if (emailResult.IsFailure)
            {
                _context.PasswordResetTokens.Remove(resetTokenEntity);
                await _context.SaveChangesAsync(ct);
                return ResultFactories.Fail(ErrorCodes.Common.OPERATION_FAILED);
            }

            await _context.SaveChangesAsync(ct);

            await LogAuditAsync(user.Id, user.Email!, AuditAction.Created, "ResetLinkSent",
                                $"Email sent successfully, expires at {tokenExpires:yyyy-MM-dd HH:mm:ss} UTC", ct);

            _logger.LogInformation("Password reset email sent successfully to {Email}, expires at {ExpiresAt}", user.Email, tokenExpires);
            return Result.Success();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", request.Email);
            return ResultFactories.Fail(ErrorCodes.Common.OPERATION_FAILED);
        }
    }



    public async Task<bool> CanRequestPasswordResetAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_settings.AllowMultipleRequests) return true;

        var todayRequests = await _context.AuditLogs
            .AsNoTracking()
            .Where(al => al.ActorId == userId &&
                         al.TargetEntityName == "PasswordReset" &&
                         al.Action == AuditAction.Created &&
                         al.Timestamp >= DateTime.UtcNow.Date)
            .CountAsync(ct);

        if (todayRequests >= _settings.MaxRequestsPerDay)
            return false;

        var lastSent = await GetLastPasswordResetRequestAsync(userId, ct);
        if (lastSent != null && DateTime.UtcNow < lastSent.Timestamp.AddMinutes(_settings.RequestCooldownMinutes))
            return false;

        return true;
    }

    private async Task<AuditLog?> GetLastPasswordResetRequestAsync(Guid userId, CancellationToken ct)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(al => al.ActorId == userId &&
                         al.TargetEntityName == "PasswordReset" &&
                         al.Action == AuditAction.Created)
            .OrderByDescending(al => al.Timestamp)
            .FirstOrDefaultAsync(ct);
    }

    private string BuildResetUrl(Guid userId, string encodedToken)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var path = _settings.ResetPasswordPath.TrimStart('/');
        return $"{baseUrl}/{path}?userId={userId}&token={encodedToken}";
    }

    private PasswordResetEmailContent CreateResetEmailContent(string userName, string resetUrl)
    {
        var t = _settings.Template;
        var subject = $"Reset Your Password - {t.CompanyName}";
        var htmlContent = PasswordResetEmailTemplateHelper.CreatePasswordResetHtml(
            userName, resetUrl, t.CompanyName, t.SupportEmail, t.WebUrl);
        var plainTextContent = PasswordResetEmailTemplateHelper.CreatePasswordResetText(
            userName, resetUrl, t.CompanyName, t.SupportEmail);
        return new PasswordResetEmailContent(subject, htmlContent, plainTextContent);
    }

    private async Task LogAuditAsync(Guid userId, string email, AuditAction action, string status, string? details, CancellationToken ct)
    {
        try
        {
            var description = details != null ? $"{status}: {details}" : status;
            _context.AuditLogs.Add(AuditLog.Create(userId, action, "PasswordReset", email, description, isSuccess: true));
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log password reset audit for user {UserId}", userId);
        }
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
                token.Revoke("Password change/reset - all sessions terminated");

            if (activeTokens.Any())
                _context.RefreshTokens.UpdateRange(activeTokens);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke tokens for user {UserId}", userId);
        }
    }

    private async Task InvalidateActiveResetTokensAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var activeTokens = await _context.PasswordResetTokens
                .Where(prt => prt.UserId == userId && !prt.IsUsed && !prt.IsExpired)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
                token.MarkAsUsed("Invalidated by new reset request");

            if (activeTokens.Any())
            {
                _context.PasswordResetTokens.UpdateRange(activeTokens);
                _logger.LogInformation("Invalidated {Count} active reset tokens for user {UserId}", activeTokens.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate active reset tokens for user {UserId}", userId);
        }
    }

    private async Task InvalidateUserResetTokens(Guid userId, CancellationToken ct)
    {
        try
        {
            var unused = await _context.PasswordResetTokens
                .Where(prt => prt.UserId == userId && !prt.IsUsed)
                .ToListAsync(ct);

            foreach (var token in unused)
                token.MarkAsUsed();

            if (unused.Any())
                _context.PasswordResetTokens.UpdateRange(unused);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate reset tokens for user {UserId}", userId);
        }
    }

 
    public async Task<int> CleanupExpiredThrottlesAsync(CancellationToken ct = default)
    {
        try
        {
            var expiredThrottles = await _context.PasswordResetTokens
                .Where(prt => prt.ThrottleUntil.HasValue && prt.ThrottleUntil.Value <= DateTime.UtcNow)
                .ToListAsync(ct);

            foreach (var token in expiredThrottles)
            {
                token.ClearThrottle();
            }

            if (expiredThrottles.Any())
            {
                _context.PasswordResetTokens.UpdateRange(expiredThrottles);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Cleaned up {Count} expired throttles", expiredThrottles.Count);
            }

            return expiredThrottles.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired throttles");
            return 0;
        }
    }

    public async Task<PasswordResetThrottleStatus> GetThrottleStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var activeTokens = await _context.PasswordResetTokens
            .Where(prt => prt.UserId == userId && !prt.IsUsed && !prt.IsExpired)
            .OrderByDescending(prt => prt.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (activeTokens == null)
            return new PasswordResetThrottleStatus(false, false, 0, null, null);

        return new PasswordResetThrottleStatus(
            true,
            activeTokens.IsThrottled,
            activeTokens.AttemptCount,
            activeTokens.ThrottleUntil,
            activeTokens.LastAttemptAt
        );
    }

    private string HashToken(string token)
    {
        Guard.AgainstNullOrEmpty(token, nameof(token));
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }
}
