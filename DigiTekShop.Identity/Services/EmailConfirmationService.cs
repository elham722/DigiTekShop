using DigiTekShop.Contracts.DTOs.EmailConfirmation;
using DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using DigiTekShop.SharedKernel.Guards;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace DigiTekShop.Identity.Services;

public class EmailConfirmationService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly EmailConfirmationSettings _settings;
    private readonly ILogger<EmailConfirmationService> _logger;

    public EmailConfirmationService(
        UserManager<User> userManager,
        IEmailSender emailSender,
        DigiTekShopIdentityDbContext context,
        IOptions<EmailConfirmationSettings> settings,
        ILogger<EmailConfirmationService> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result> SendConfirmationEmailAsync(User user)
    {
        if (!_settings.RequireEmailConfirmation || user.EmailConfirmed)
            return Result.Success();

        Guard.AgainstNullOrEmpty(user.Email, nameof(user.Email));

        if (_settings.AllowResendConfirmation && !await CanResendConfirmationAsync(user.Id))
            return Result.Failure($"Please wait {_settings.ResendCooldownMinutes} minutes before requesting another email.");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = BuildConfirmationUrl(user.Id, token);

        var content = CreateConfirmationEmailContent(user.UserName ?? "User", confirmationUrl);
        var sendResult = await _emailSender.SendEmailAsync(user.Email, content.Subject, content.HtmlContent, content.PlainTextContent);

        if (sendResult.IsFailure)
            return Result.Failure("Failed to send confirmation email.");

        await LogAuditAsync(user.Id, user.Email!, AuditAction.Created, "Sent");
        _logger.LogInformation("Confirmation email sent to {Email}", user.Email);

        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(string userId, string encodedToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure("Invalid user ID.");
        if (user.EmailConfirmed) return Result.Success();

        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
            return Result.Failure($"Confirmation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await LogAuditAsync(user.Id, user.Email!, AuditAction.Updated, "Confirmed");
        return Result.Success();
    }

    public async Task<bool> CanResendConfirmationAsync(Guid userId)
    {
        if (!_settings.AllowResendConfirmation) return false;

        var lastSent = await _context.AuditLogs
            .Where(al => al.UserId == userId && al.EntityName == "EmailConfirmation")
            .OrderByDescending(al => al.Timestamp)
            .FirstOrDefaultAsync();

        return lastSent == null || DateTime.UtcNow >= lastSent.Timestamp.AddMinutes(_settings.ResendCooldownMinutes);
    }

    #region Helpers

    private string BuildConfirmationUrl(Guid userId, string token)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var path = _settings.ConfirmEmailPath.TrimStart('/');
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return $"{baseUrl}/{path}?userId={userId}&token={encodedToken}";
    }

    private EmailConfirmationContent CreateConfirmationEmailContent(string userName, string url)
    {
        var t = _settings.Template;
        return new EmailConfirmationContent(
            $"Confirm Your Email - {t.CompanyName}",
            $"<p>Hello {userName}, please confirm: <a href='{url}'>Confirm Email</a></p>",
            $"Hello {userName}, confirm your email: {url}"
        );
    }

    private async Task LogAuditAsync(Guid userId, string email, AuditAction action, string status)
    {
        try
        {
            _context.AuditLogs.Add(AuditLog.Create(userId, action, "EmailConfirmation", email, status, isSuccess: true));
            await _context.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed."); }
    }

    #endregion
}


