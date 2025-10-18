using DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;
using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.Enums.Audit;
using FluentValidation;
using Microsoft.AspNetCore.WebUtilities;

namespace DigiTekShop.Identity.Services;

public class EmailConfirmationService : IEmailConfirmationService
{
    private const string AuditTarget = "EmailConfirmation";

    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateService _template;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly EmailConfirmationSettings _settings;
    private readonly ILogger<EmailConfirmationService> _logger;

    public EmailConfirmationService(
        UserManager<User> userManager,
        IEmailSender emailSender,
        IEmailTemplateService template,
        DigiTekShopIdentityDbContext context,
        IOptions<EmailConfirmationSettings> settings,
        ILogger<EmailConfirmationService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _template = template ?? throw new ArgumentNullException(nameof(template));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> SendAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure("UserId is required.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Result.Failure("User not found.");
        if (string.IsNullOrWhiteSpace(user.Email)) return Result.Failure("Email is not set.");

        return await SendConfirmationEmailAsync(user, ct);
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return Result.Failure("Invalid user.");
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

        var idResult = await _userManager.ConfirmEmailAsync(user, identityToken);
        if (!idResult.Succeeded)
            return Result.Failure(string.Join(", ", idResult.Errors.Select(e => e.Description)));

        await LogAuditAsync(user.Id, user.Email!, AuditAction.Updated, "Confirmed", ct);
        _logger.LogInformation("Email confirmed for user {UserId} ({Email})", user.Id, MaskEmail(user.Email!));
        return Result.Success();
    }

    public async Task<Result> ResendAsync(ResendEmailConfirmationRequestDto request, CancellationToken ct = default)
    {
        var raw = request.Email.Trim();
        var normalizedLookup = _userManager.NormalizeEmail(raw) ?? raw.ToUpperInvariant();

        var user = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.NormalizedEmail == normalizedLookup)
            .FirstOrDefaultAsync(ct);

        if (user is null || user.EmailConfirmed) return Result.Success();

        return await SendConfirmationEmailAsync(user, ct);
    }

   
    private async Task<Result> SendConfirmationEmailAsync(User user, CancellationToken ct)
    {
        if (!_settings.RequireEmailConfirmation || user.EmailConfirmed)
            return Result.Success();

        if (string.IsNullOrWhiteSpace(user.Email))
            return Result.Failure("Email is not set.");

        if (!ValidateUrlSettings(out var urlError))
            return Result.Failure(urlError!);

        if (_settings.AllowResendConfirmation && !await CanResendConfirmationAsync(user.Id, ct))
        {
            var wait = _settings.ResendCooldown.TotalMinutes >= 1
                ? $"{_settings.ResendCooldown.TotalMinutes:N0} minutes"
                : $"{_settings.ResendCooldown.TotalSeconds:N0} seconds";
            return Result.Failure($"Please wait {wait} before requesting again.");
        }


        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = BuildConfirmationUrl(user.Id, token);

        var company = _settings.Template?.CompanyName ?? "DigiTekShop";
        var content = _template.BuildEmailConfirmation(confirmationUrl, company);

        var sendResult = await _emailSender.SendEmailAsync(user.Email!, content.Subject, content.HtmlContent, content.PlainTextContent);
        if (sendResult.IsFailure)
        {
            _logger.LogWarning("Failed to send confirmation email to {Email}: {Error}", MaskEmail(user.Email!), sendResult.Errors);
            return Result.Failure("Failed to send confirmation email.");
        }

        await LogAuditAsync(user.Id, user.Email!, AuditAction.Created, "Sent", ct);
        _logger.LogInformation("Confirmation email sent to {Email}", MaskEmail(user.Email!));
        return Result.Success();
    }

    private async Task<bool> CanResendConfirmationAsync(Guid userId, CancellationToken ct)
    {
        var lastSent = await _context.AuditLogs
            .Where(al => al.ActorId == userId && al.TargetEntityName == AuditTarget && al.IsSuccess)
            .OrderByDescending(al => al.Timestamp)
            .Select(al => al.Timestamp)
            .FirstOrDefaultAsync(ct);

        return lastSent == default || DateTime.UtcNow >= lastSent.Add(_settings.ResendCooldown);
    }

    private string BuildConfirmationUrl(Guid userId, string token)
    {
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = _settings.BaseUrl?.TrimEnd('/') ?? throw new InvalidOperationException("EmailConfirmation.BaseUrl is required.");
        var path = _settings.ConfirmEmailPath?.TrimStart('/') ?? "account/confirm-email";

        return QueryHelpers.AddQueryString($"{baseUrl}/{path}", new Dictionary<string, string?>
        {
            ["userId"] = userId.ToString(),
            ["token"] = encodedToken
        });
    }

    private bool ValidateUrlSettings(out string? error)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            error = "Email confirmation base URL is not configured.";
            return false;
        }
        error = null;
        return true;
    }

    private async Task LogAuditAsync(Guid userId, string email, AuditAction action, string status, CancellationToken ct)
    {
        try
        {
            _context.AuditLogs.Add(AuditLog.Create(userId, action, AuditTarget, email, status, isSuccess: true));
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log failed for user {UserId}", userId);
        }
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        var local = email[..at];
        var domain = email[(at + 1)..];
        var maskedLocal = local.Length <= 2 ? local[0] + "*" : $"{local[0]}***{local[^1]}";
        return $"{maskedLocal}@{domain}";
    }
}
