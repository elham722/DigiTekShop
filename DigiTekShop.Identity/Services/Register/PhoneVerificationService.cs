using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;
using DigiTekShop.Contracts.Options.Phone;
using DigiTekShop.SharedKernel.Utilities.Security;
using DigiTekShop.SharedKernel.Utilities.Text;
using System.Text.RegularExpressions;

namespace DigiTekShop.Identity.Services.Register;

public sealed class PhoneVerificationService : IPhoneVerificationService
{
    private static class Events
    {
        public static readonly EventId Send = new(43001, nameof(SendVerificationCodeAsync));
        public static readonly EventId Verify = new(43002, nameof(VerifyCodeAsync));
        public static readonly EventId CanRe = new(43003, nameof(CanResendCodeAsync));
        public static readonly EventId Persist = new(43004, "PersistVerification");
    }

    private readonly UserManager<User> _users;
    private readonly IPhoneSender _sender;
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IDateTimeProvider _time;
    private readonly PhoneVerificationOptions _opts;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<PhoneVerificationService> _log;

    public PhoneVerificationService(
        UserManager<User> userManager,
        IPhoneSender phoneSender,
        DigiTekShopIdentityDbContext db,
        IDateTimeProvider time,
        IOptions<PhoneVerificationOptions> settings,
        IRateLimiter rateLimiter,
        ILogger<PhoneVerificationService> logger)
    {
        _users = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _sender = phoneSender ?? throw new ArgumentNullException(nameof(phoneSender));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _opts = settings?.Value ?? new PhoneVerificationOptions();
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> SendVerificationCodeAsync(Guid userId, string phoneNumber, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return Result.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

        var normalized = Normalization.NormalizePhone(phoneNumber);
        return await SendCoreAsync(user, normalized, ct);
    }

    public async Task<Result> VerifyCodeAsync(string userId, string code, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid) || uid == Guid.Empty)
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var user = await _users.FindByIdAsync(uid.ToString());
        if (user is null || user.IsDeleted)
            return Result.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

        return await VerifyCoreAsync(user, code, ct);
    }

    public async Task<bool> CanResendCodeAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return true;

        var last = await _db.PhoneVerifications
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        return last == default || _time.UtcNow >= last.Add(_opts.ResendCooldown);
    }

    #region Core

    private async Task<Result> SendCoreAsync(User user, string phoneNumber, CancellationToken ct)
    {
        if (!_opts.RequirePhoneConfirmation || user.PhoneNumberConfirmed)
            return Result.Success();

        if (!IsPhoneAllowed(phoneNumber))
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        if (_opts.Security.RequireUniquePhoneNumbers)
        {
            var userPhoneNorm = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : Normalization.NormalizePhone(user.PhoneNumber);
            if (userPhoneNorm is null || !string.Equals(userPhoneNorm, phoneNumber, StringComparison.Ordinal))
                return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED, "Phone number does not match the registered number.");
        }

        
        var rlKey = $"phone_ver:{user.Id}";
        var allowed = await _rateLimiter.ShouldAllowAsync(
            rlKey,
            _opts.Security.MaxRequestsPerHour,
            TimeSpan.FromHours(1),
            ct);
        if (!allowed)
        {
            _log.LogWarning(Events.Send, "Rate limit exceeded. user={UserId}", user.Id);
            return Result.Failure(ErrorCodes.Common.OPERATION_FAILED, "Too many verification requests. Please try again later.");
        }

        
        if (_opts.AllowResendCode && !await CanResendCodeAsync(user.Id, ct))
        {
            var msg = Humanize(_opts.ResendCooldown);
            return Result.Failure(ErrorCodes.Common.OPERATION_FAILED, $"Please wait {msg} before requesting another code.");
        }

       
        var code = GenerateNumericCode(_opts.CodeLength);
        var hash = BCrypt.Net.BCrypt.HashPassword(code);
        var now = _time.UtcNow;
        var expires = now.Add(_opts.CodeValidity);

        
        await UpsertVerificationAsync(user.Id, hash, expires, phoneNumber, now, ct);

        
        var templateName = _opts.Template?.OtpTemplateName ?? "default_otp";
        var send = await _sender.SendCodeAsync(phoneNumber, code, templateName);
        if (send.IsFailure)
        {
            _log.LogWarning(Events.Send, "SMS send failed. user={UserId}, phone={Phone}",
                user.Id, SensitiveDataMasker.MaskPhone(phoneNumber));
            return Result.Failure(ErrorCodes.Common.OPERATION_FAILED, "Failed to send verification SMS.");
        }

        _log.LogInformation(Events.Send, "Verification code sent. user={UserId}, phone={Phone}",
            user.Id, SensitiveDataMasker.MaskPhone(phoneNumber));
        return Result.Success();
    }

    private async Task<Result> VerifyCoreAsync(User user, string enteredCode, CancellationToken ct)
    {
        var pv = await _db.PhoneVerifications
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (pv is null)
            return Result.Failure(ErrorCodes.Common.INTERNAL_ERROR, "No code found.");

        var now = _time.UtcNow;

        if (pv.IsExpired(now))
            return Result.Failure(ErrorCodes.Common.INTERNAL_ERROR, "Code expired.");

        if (pv.Attempts >= _opts.MaxAttempts)
            return Result.Failure(ErrorCodes.Common.INTERNAL_ERROR, "Too many attempts.");

        var ok = BCrypt.Net.BCrypt.Verify(enteredCode, pv.CodeHash);
        if (!ok)
        {
            pv.TryIncrementAttempts(_opts.MaxAttempts);
            await _db.SaveChangesAsync(ct);
            return Result.Failure(ErrorCodes.Common.OPERATION_FAILED, "Invalid code.");
        }

        
        pv.MarkAsVerified(now);

        if (!string.IsNullOrWhiteSpace(pv.PhoneNumber) &&
            string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            user.PhoneNumber = pv.PhoneNumber;
        }

        user.PhoneNumberConfirmed = true;

        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            var msg = string.Join(", ", update.Errors.Select(e => e.Description));
            _log.LogWarning(Events.Verify, "User update failed after phone verify. user={UserId}, err={Err}", user.Id, msg);
            return Result.Failure(ErrorCodes.Identity.INVALID_PHONE, "Failed to confirm phone.");
        }

        await _db.SaveChangesAsync(ct);

        _log.LogInformation(Events.Verify, "Phone verified. user={UserId}, phone={Phone}",
            user.Id, SensitiveDataMasker.MaskPhone(user.PhoneNumber ?? pv.PhoneNumber ?? "n/a"));
        return Result.Success();
    }

    #endregion

    #region Persistence

    private async Task UpsertVerificationAsync(
        Guid userId,
        string hash,
        DateTime expiresUtc,
        string? phoneNumber,
        DateTime createdAtUtc,
        CancellationToken ct)
    {
        try
        {
            var last = await _db.PhoneVerifications
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (last is null)
            {
                var pv = PhoneVerification.Create(userId, hash, createdAtUtc, expiresUtc, phoneNumber);
                _db.PhoneVerifications.Add(pv);
            }
            else
            {
                last.ResetCode(hash, createdAtUtc, expiresUtc, phoneNumber);
            }

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(Events.Persist, ex, "Persist verification failed. user={UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Validation & Utils

    private bool IsPhoneAllowed(string phone)
    {
        var p = Normalization.NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(p)) return false;

        var pattern = _opts.Security.AllowedPhonePattern;
        if (!string.IsNullOrWhiteSpace(pattern))
            return Regex.IsMatch(p, pattern);

        return Regex.IsMatch(p, @"^\+?\d{8,15}$");
    }

    private static string GenerateNumericCode(int length)
    {
        if (length <= 0) length = 6;
        Span<byte> bytes = stackalloc byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        Span<char> chars = stackalloc char[length];
        for (int i = 0; i < length; i++)
            chars[i] = (char)('0' + (bytes[i] % 10));
        return new string(chars);
    }

    private static string Humanize(TimeSpan span)
        => span.TotalMinutes >= 1
            ? $"{span.TotalMinutes:N0} minutes"
            : $"{span.TotalSeconds:N0} seconds";

    #endregion
}
