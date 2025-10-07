using DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Identity.Options;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;

namespace DigiTekShop.Identity.Services;

public class EmailConfirmationService : IEmailConfirmationService
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

    public async Task<Result> SendAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Result.Failure("User not found");
        if (string.IsNullOrWhiteSpace(user.Email)) return Result.Failure("Email not set");

        return await SendConfirmationEmailAsync(user, ct);

    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure("Invalid confirm request");

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null) return Result.Failure("Invalid user ID.");
        if (user.EmailConfirmed) return Result.Success();

        string identityToken;
        try
        {
            identityToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            return Result.Failure("Invalid token.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, identityToken);
        if (!result.Succeeded)
            return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        await LogAuditAsync(user.Id, user.Email!, AuditAction.Updated, "Confirmed", ct);
        return Result.Success();
    }

    public async Task<Result> ResendAsync(ResendEmailConfirmationRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure("Email is required.");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return Result.Success(); 
        if (user.EmailConfirmed) return Result.Success();

        return await SendConfirmationEmailAsync(user, ct);
    }

   
    private async Task<Result> SendConfirmationEmailAsync(User user, CancellationToken ct)
    {
        if (!_settings.RequireEmailConfirmation || user.EmailConfirmed)
            return Result.Success();

        if (string.IsNullOrWhiteSpace(user.Email))
            return Result.Failure("Email not set");

        if (_settings.AllowResendConfirmation && !await CanResendConfirmationAsync(user.Id, ct))
            return Result.Failure($"Please wait {_settings.ResendCooldownMinutes} minutes...");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = BuildConfirmationUrl(user.Id, token);

        var content = CreateConfirmationEmailContent(user.UserName ?? "User", confirmationUrl);
        var sendResult = await _emailSender.SendEmailAsync(user.Email, content.Subject, content.HtmlContent,
            content.PlainTextContent);
        if (sendResult.IsFailure) return Result.Failure("Failed to send confirmation email.");

        await LogAuditAsync(user.Id, user.Email, AuditAction.Created, "Sent", ct);
        return Result.Success();
    }

    private async Task<bool> CanResendConfirmationAsync(Guid userId, CancellationToken ct)
    {
        var lastSent = await _context.AuditLogs
            .Where(al => al.UserId == userId && al.EntityName == "EmailConfirmation")
            .OrderByDescending(al => al.Timestamp)
            .FirstOrDefaultAsync(ct);

        return lastSent == null ||
               DateTime.UtcNow >= lastSent.Timestamp.AddMinutes(_settings.ResendCooldownMinutes);
    }

    private string BuildConfirmationUrl(Guid userId, string token)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var path = _settings.ConfirmEmailPath.TrimStart('/');
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return QueryHelpers.AddQueryString($"{baseUrl}/{path}",
            new Dictionary<string, string?> { ["userId"] = userId.ToString(), ["token"] = encodedToken });
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

    private async Task LogAuditAsync(Guid userId, string email, AuditAction action, string status,
        CancellationToken ct)
    {
        try
        {
            _context.AuditLogs.Add(AuditLog.Create(userId, action, "EmailConfirmation", email, status,
                isSuccess: true));
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log failed.");
        }
    }
}
    