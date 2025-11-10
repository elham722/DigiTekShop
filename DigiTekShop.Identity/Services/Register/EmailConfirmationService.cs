using DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;
using DigiTekShop.Contracts.Abstractions.Identity.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.Options.Email;
using DigiTekShop.SharedKernel.Enums.Audit;
using DigiTekShop.SharedKernel.Utilities.Security;
using DigiTekShop.SharedKernel.Utilities.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace DigiTekShop.Identity.Services.Register;

public sealed class EmailConfirmationService : IEmailConfirmationService
{
    private static class Events
    {
        public static readonly EventId Send = new(42001, nameof(SendAsync));
        public static readonly EventId Confirm = new(42002, nameof(ConfirmEmailAsync));
        public static readonly EventId Resend = new(42003, nameof(ResendAsync));
        public static readonly EventId Audit = new(42004, nameof(LogAuditAsync));
    }

    private const string AuditTarget = "EmailConfirmation";

    private readonly UserManager<User> _users;
    private readonly IEmailSender _email;
    private readonly IEmailTemplateService _template;
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IDateTimeProvider _time;
    private readonly EmailConfirmationOptions _opts;
    private readonly ILogger<EmailConfirmationService> _log;

    public EmailConfirmationService(
        UserManager<User> users,
        IEmailSender email,
        IEmailTemplateService template,
        DigiTekShopIdentityDbContext db,
        IDateTimeProvider time,
        IOptions<EmailConfirmationOptions> opts,
        ILogger<EmailConfirmationService> log)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _email = email ?? throw new ArgumentNullException(nameof(email));
        _template = template ?? throw new ArgumentNullException(nameof(template));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _opts = opts?.Value ?? new EmailConfirmationOptions();
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public async Task<Result> SendAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return Result.Success();                
        if (user.EmailConfirmed) return Result.Success();      
        if (string.IsNullOrWhiteSpace(user.Email)) return Result.Success();

        return await SendCoreAsync(user, ct);
    }

    public async Task<Result> ResendAsync(ResendEmailConfirmationRequestDto request, CancellationToken ct = default)
    {
        var raw = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return Result.Success();

        var normalized = Normalization.Normalize(raw);
        var normLookup = _users.NormalizeEmail(normalized) ?? normalized.ToUpperInvariant();

        var user = await _users.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normLookup, ct);

        if (user is null || user.EmailConfirmed) return Result.Success(); 

        return await SendCoreAsync(user, ct);
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(request.UserId.ToString());
        if (user is null) return Result.Success();                
        if (user.EmailConfirmed) return Result.Success();

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            _log.LogWarning(Events.Confirm, "Invalid confirm token format. user={UserId}", request.UserId);
            return Result.Failure(ErrorCodes.Identity.INVALID_TOKEN);
        }

        var res = await _users.ConfirmEmailAsync(user, token);
        if (!res.Succeeded)
        {
            var msg = string.Join(", ", res.Errors.Select(e => e.Description));
            _log.LogWarning(Events.Confirm, "Email confirm failed. user={UserId}, errors={Errors}", user.Id, msg);
            return Result.Failure(ErrorCodes.Identity.INVALID_EMAIL);
        }

        await LogAuditAsync(user.Id, user.Email!, AuditAction.Updated, "Confirmed", ct);
        _log.LogInformation(Events.Confirm, "Email confirmed. user={UserId}, email={Email}", user.Id, SensitiveDataMasker.MaskEmail(user.Email!));
        return Result.Success();
    }

    #region Private Core

    private async Task<Result> SendCoreAsync(User user, CancellationToken ct)
    {
        if (!_opts.RequireEmailConfirmation) return Result.Success();
        if (user.EmailConfirmed) return Result.Success();
        if (!ValidateUrlSettings(out var err)) return Result.Failure(err!);

        if (_opts.AllowResendConfirmation && !await CanResendAsync(user.Id, ct))
        {
            var wait = Humanize(_opts.ResendCooldown);
            return Result.Failure($"Please wait {wait} before requesting again.");
        }

        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var url = BuildConfirmationUrl(user.Id, token);

        var company = _opts.Template?.CompanyName ?? "DigiTekShop";
        var content = _template.BuildEmailConfirmation(url, company);

        var sent = await _email.SendEmailAsync(user.Email!, content.Subject, content.HtmlContent, content.PlainTextContent);
        if (sent.IsFailure)
        {
            _log.LogWarning(Events.Send, "Send confirm email failed. user={UserId}, email={Email}", user.Id, SensitiveDataMasker.MaskEmail(user.Email!));
            return Result.Failure("Failed to send confirmation email.");
        }

        await LogAuditAsync(user.Id, user.Email!, AuditAction.Created, "Sent", ct);
        _log.LogInformation(Events.Send, "Confirmation email sent. user={UserId}, email={Email}", user.Id, SensitiveDataMasker.MaskEmail(user.Email!));
        return Result.Success();
    }

    private async Task<bool> CanResendAsync(Guid userId, CancellationToken ct)
    {
        if (!_opts.AllowResendConfirmation) return true;

        var last = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.ActorId == userId && a.TargetEntityName == AuditTarget && a.IsSuccess)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => a.Timestamp)
            .FirstOrDefaultAsync(ct);

        var now = DateTimeOffset.UtcNow;
        return last == default || now >= last.Add(_opts.ResendCooldown);
    }

    private string BuildConfirmationUrl(Guid userId, string token)
    {
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = _opts.BaseUrl?.TrimEnd('/')
            ?? throw new InvalidOperationException("EmailConfirmation.BaseUrl is required.");

        var path = string.IsNullOrWhiteSpace(_opts.ConfirmEmailPath)
            ? "api/v1/account/confirm-email"
            : _opts.ConfirmEmailPath!.TrimStart('/');

        return QueryHelpers.AddQueryString($"{baseUrl}/{path}", new Dictionary<string, string?>
        {
            ["userId"] = userId.ToString(),
            ["token"] = encodedToken
        });
    }

    private bool ValidateUrlSettings(out string? error)
    {
        if (string.IsNullOrWhiteSpace(_opts.BaseUrl))
        { error = "Email confirmation base URL is not configured."; return false; }

        if (!Uri.TryCreate(_opts.BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        { error = "Email confirmation base URL must be absolute (http/https)."; return false; }

        error = null; return true;
    }

    private async Task LogAuditAsync(Guid userId, string email, AuditAction action, string status, CancellationToken ct)
    {
        try
        {
            var log = AuditLog.Create(
                actorId: userId,
                action: action,
                targetEntityName: AuditTarget,
                targetEntityId: email,
                newValueJson: status,
                isSuccess: true);
            
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(Events.Audit, ex, "Audit log failed. user={UserId}", userId);
        }
    }

    private static string Humanize(TimeSpan span)
        => span.TotalMinutes >= 1 ? $"{span.TotalMinutes:N0} minutes" : $"{span.TotalSeconds:N0} seconds";

    #endregion

}
