using DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender;
using DigiTekShop.Identity.Models;
using DigiTekShop.Identity.Helpers.EmailTemplates;
using DigiTekShop.Identity.Exceptions.Common;
using DigiTekShop.SharedKernel.Exceptions.Common;
using DigiTekShop.SharedKernel.Results;
using DigiTekShop.SharedKernel.Guards;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using DigiTekShop.Contracts.DTOs.ResetPassword;

namespace DigiTekShop.Identity.Services;

/// <summary>
/// Service for handling password reset functionality with improved security and auditing
/// </summary>
public class PasswordResetService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly PasswordResetSettings _settings;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        UserManager<User> userManager,
        IEmailSender emailSender,
        DigiTekShopIdentityDbContext context,
        IOptions<PasswordResetSettings> settings,
        ILogger<PasswordResetService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends password reset link to user's email with token expiration enforcement
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Operation result</returns>
    public async Task<Result> SendResetLinkAsync(ForgotPasswordDto request, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Validate settings
            if (!_settings.IsEnabled)

            // Validate input
            Guard.AgainstNullOrEmpty(request.Email, nameof(request.Email));
            Guard.AgainstInvalidEmail(request.Email);

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || user.IsDeleted || !user.EmailConfirmed)
            {
                // Don't reveal if user exists or not for security
                _logger.LogWarning("Password reset requested for non-existent or inactive email: {Email}", request.Email);
                return Result.Success(); // Always return success for security
            }

            // Check cooldown and request limits
            if (!await CanRequestPasswordResetAsync(user.Id))
            {
                var lastRequest = await GetLastPasswordResetRequestAsync(user.Id);
                var remainingMinutes = lastRequest?.Timestamp.AddMinutes(_settings.RequestCooldownMinutes).Subtract(DateTime.UtcNow).Minutes ?? 0;
                
                return Result.Failure(
                    IdentityErrorMessages.GetMessage(IdentityErrorCodes.PASSWORD_RESET_COOLDOWN_ACTIVE), 
                    IdentityErrorCodes.PASSWORD_RESET_COOLDOWN_ACTIVE);
            }

            // Generate ASP.NET Identity password reset token
            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Hash the token for storage
            var tokenHash = HashToken(identityToken);
            var tokenExpires = DateTime.UtcNow.AddMinutes(_settings.TokenValidityMinutes);

            // Create and store password reset token entity
            var resetTokenEntity = PasswordResetToken.Create(user.Id, tokenHash, tokenExpires, ipAddress, userAgent);
            _context.PasswordResetTokens.Add(resetTokenEntity);

            // Build reset URL with our own token identifier
            var resetUrl = BuildResetUrl(user.Id, WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(identityToken)));

            // Create email content
            var content = CreateResetEmailContent(user.UserName ?? "User", resetUrl);

            // Send email
            var emailResult = await _emailSender.SendEmailAsync(user.Email!, content.Subject, content.HtmlContent, content.PlainTextContent);
            if (emailResult.IsFailure)
            {
                // Remove the token entity if email sending failed
                _context.PasswordResetTokens.Remove(resetTokenEntity);
                await _context.SaveChangesAsync();
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.OPERATION_FAILED), IdentityErrorCodes.OPERATION_FAILED);
            }

            await _context.SaveChangesAsync();

            // Log detailed audit
            await LogAuditAsync(user.Id, user.Email!, AuditAction.Created, "ResetLinkSent", $"Email sent successfully, expires at {tokenExpires:yyyy-MM-dd HH:mm:ss} UTC");

            _logger.LogInformation("Password reset email sent successfully to {Email}, expires at {ExpiresAt}", user.Email, tokenExpires);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", request.Email);
            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.OPERATION_FAILED), IdentityErrorCodes.OPERATION_FAILED);
        }
    }

    /// <summary>
    /// Resets user's password using the provided token with expiration enforcement
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <param name="ipAddress">Client IP address for audit</param>
    /// <returns>Operation result</returns>
    public async Task<Result> ResetPasswordAsync(ResetPasswordDto request, string? ipAddress = null)
    {
        try
        {
            // Validate settings
            if (!_settings.IsEnabled)
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.PASSWORD_RESET_DISABLED), IdentityErrorCodes.PASSWORD_RESET_DISABLED);

            // Validate input
            Guard.AgainstNullOrEmpty(request.UserId, nameof(request.UserId));
            Guard.AgainstNullOrEmpty(request.Token, nameof(request.Token));
            Guard.AgainstNullOrEmpty(request.NewPassword, nameof(request.NewPassword));

            // Find user
            if (!Guid.TryParse(request.UserId, out var userId))
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_USER_FOR_PASSWORD_RESET), IdentityErrorCodes.INVALID_USER_FOR_PASSWORD_RESET);

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null || user.IsDeleted)
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_USER_FOR_PASSWORD_RESET), IdentityErrorCodes.INVALID_USER_FOR_PASSWORD_RESET);

            // Decode token
            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch (Exception)
            {
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_RESET_TOKEN), IdentityErrorCodes.INVALID_RESET_TOKEN);
            }

            // Find and validate stored token
            var tokenHash = HashToken(decodedToken);
            var storedToken = await _context.PasswordResetTokens
                .Where(prt => prt.UserId == userId && prt.TokenHash == tokenHash)
                .FirstOrDefaultAsync();

            if (storedToken == null)
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_RESET_TOKEN), IdentityErrorCodes.INVALID_RESET_TOKEN);

            // Check token expiration using our stored expiration time
            if (storedToken.IsExpired)
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.RESET_TOKEN_EXPIRED), IdentityErrorCodes.RESET_TOKEN_EXPIRED);

            if (storedToken.IsUsed)
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.INVALID_RESET_TOKEN), IdentityErrorCodes.INVALID_RESET_TOKEN);

            // Reset password using ASP.NET Identity
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                _logger.LogWarning("Password reset failed for user {UserId}. Errors: {Errors}", userId, string.Join(", ", errors));
                return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.PASSWORD_RESET_FAILED), IdentityErrorCodes.PASSWORD_RESET_FAILED);
            }

            // Mark token as used
            storedToken.MarkAsUsed(ipAddress);
            _context.PasswordResetTokens.Update(storedToken);

            // Revoke all refresh tokens (security measure)
            await RevokeAllUserTokensAsync(userId);

            // Invalidate all other unused reset tokens for this user
            await InvalidateUserResetTokens(userId);

            await _context.SaveChangesAsync();

            // Log password reset success
            await LogAuditAsync(userId, user.Email!, AuditAction.Updated, "PasswordResetCompleted", $"Password reset completed successfully from IP {ipAddress}");

            _logger.LogInformation("Password reset completed successfully for user {UserId} from IP {IpAddress}", userId, ipAddress);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", request.UserId);
            return Result.Failure(IdentityErrorMessages.GetMessage(IdentityErrorCodes.PASSWORD_RESET_FAILED), IdentityErrorCodes.PASSWORD_RESET_FAILED);
        }
    }

    /// <summary>
    /// Checks if user can request password reset (cooldown and limits)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if user can request password reset</returns>
    public async Task<bool> CanRequestPasswordResetAsync(Guid userId)
    {
        if (!_settings.AllowMultipleRequests) return true;

        // Check daily limit by counting audit logs
        var todayRequests = await _context.AuditLogs
            .Where(al => al.UserId == userId &&
                        al.EntityName == "PasswordReset" &&
                        al.Action == AuditAction.Created &&
                        al.Timestamp >= DateTime.UtcNow.Date)
            .CountAsync();

        if (todayRequests >= _settings.MaxRequestsPerDay)
            return false;

        // Check cooldown from audit logs
        var lastSent = await GetLastPasswordResetRequestAsync(userId);
        if (lastSent != null && DateTime.UtcNow < lastSent.Timestamp.AddMinutes(_settings.RequestCooldownMinutes))
            return false;

        return true;
    }

    #region Private Helpers

    private async Task<AuditLog?> GetLastPasswordResetRequestAsync(Guid userId)
    {
        return await _context.AuditLogs
            .Where(al => al.UserId == userId &&
                        al.EntityName == "PasswordReset" &&
                        al.Action == AuditAction.Created)
            .OrderByDescending(al => al.Timestamp)
            .FirstOrDefaultAsync();
    }

    private DateTime GetTokenExpirationTime(DateTime? lastResetTime)
    {
        var baseTime = lastResetTime ?? DateTime.UtcNow;
        return baseTime.AddMinutes(_settings.TokenValidityMinutes);
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

    private async Task LogAuditAsync(Guid userId, string email, AuditAction action, string status, string? details = null)
    {
        try
        {
            var description = details != null ? $"{status}: {details}" : status;
            _context.AuditLogs.Add(AuditLog.Create(userId, action, "PasswordReset", email, description, isSuccess: true));
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log password reset audit for user {UserId}", userId);
        }
    }

    private async Task RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.Revoke("Password reset - all sessions terminated");
            }

            if (activeTokens.Any())
            {
                _context.RefreshTokens.UpdateRange(activeTokens);
                
                _logger.LogInformation("Revoked {TokenCount} refresh tokens for user {UserId} due to password reset", 
                    activeTokens.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke tokens for user {UserId}", userId);
        }
    }

    private async Task InvalidateUserResetTokens(Guid userId)
    {
        try
        {
            var unusedTokens = await _context.PasswordResetTokens
                .Where(prt => prt.UserId == userId && !prt.IsUsed)
                .ToListAsync();

            foreach (var token in unusedTokens)
            {
                token.MarkAsUsed();
            }

            if (unusedTokens.Any())
            {
                _context.PasswordResetTokens.UpdateRange(unusedTokens);
                _logger.LogInformation("Invalidated {TokenCount} unused reset tokens for user {UserId}", 
                    unusedTokens.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate reset tokens for user {UserId}", userId);
        }
    }

    private string HashToken(string token)
    {
        Guard.AgainstNullOrEmpty(token, nameof(token));
        
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }

    #endregion
}