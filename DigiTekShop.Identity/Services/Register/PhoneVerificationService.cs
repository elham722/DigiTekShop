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
        public static readonly EventId RateLim = new(43005, "RateLimit");
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

        // نرمال‌سازی/اعتبارسنجی
        var normalized = Normalization.NormalizePhone(phoneNumber);
        if (!IsPhoneAllowed(normalized))
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        // اگر RequireUniquePhoneNumbers فعاله، باید با شماره‌ی کاربر match باشد
        if (_opts.Security.RequireUniquePhoneNumbers)
        {
            var userPhoneNorm = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : Normalization.NormalizePhone(user.PhoneNumber);
            if (userPhoneNorm is null || !string.Equals(userPhoneNorm, normalized, StringComparison.Ordinal))
                return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED, "Phone number does not match the registered number.");
        }

        // اگر از قبل تأیید شده، موفق برگردون
        if (!_opts.RequirePhoneConfirmation || user.PhoneNumberConfirmed)
            return Result.Success();

        // Rate limit (per user)
        var rlKey = $"phone_ver:{user.Id}";
        var decision = await _rateLimiter.ShouldAllowAsync(rlKey, _opts.Security.MaxRequestsPerHour, TimeSpan.FromHours(1), ct);
        if (!decision.Allowed)
        {
            _log.LogWarning(Events.RateLim, "Phone verify rate limited. user={UserId}, retryAfter={Retry}s",
                user.Id, decision.Ttl?.TotalSeconds);
            return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED, "Too many verification requests. Please try again later.");
        }

        // Resend cooldown
        if (_opts.AllowResendCode && !await CanResendCodeAsync(user.Id, ct))
        {
            var msg = Humanize(_opts.ResendCooldown);
            return Result.Failure(ErrorCodes.Common.OPERATION_FAILED, $"Please wait {msg} before requesting another code.");
        }

        // ساخت/آپدیت کد
        var now = _time.UtcNow;
        var expires = now.Add(_opts.CodeValidity);
        var code = GenerateNumericCode(_opts.CodeLength);
        var hash = BCrypt.Net.BCrypt.HashPassword(code);

        // Find or create PhoneVerification
        // With unique filtered index (IsVerified = 0), we enforce "only one active OTP" in application layer
        var purpose = SharedKernel.Enums.Verification.VerificationPurpose.Signup;
        var channel = SharedKernel.Enums.Verification.VerificationChannel.Sms;

        // Use a transaction to prevent race conditions with unique index
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        PhoneVerification pv;
        try
        {
            // Find any unverified record (expired or not) for this phone/purpose/channel
            var existing = await _db.PhoneVerifications
                .Where(p => p.PhoneNumberNormalized == normalized &&
                           p.Purpose == purpose &&
                           p.Channel == channel &&
                           !p.IsVerified)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (existing != null)
            {
                // If there's an active (non-expired) record, reset it
                if (existing.ExpiresAtUtc > now)
                {
                    existing.ResetCode(
                        newHash: hash,
                        newExpiresAtUtc: expires,
                        phoneNumber: normalized);
                    pv = existing;
                }
                else
                {
                    // If expired, delete it to make room for new record
                    _db.PhoneVerifications.Remove(existing);
                    pv = PhoneVerification.CreateForUser(
                        userId: user.Id,
                        codeHash: hash,
                        expiresAtUtc: expires,
                        phoneNumber: normalized,
                        purpose: purpose,
                        channel: channel);
                    _db.PhoneVerifications.Add(pv);
                }
            }
            else
            {
                // No existing record, create new one
                pv = PhoneVerification.CreateForUser(
                    userId: user.Id,
                    codeHash: hash,
                    expiresAtUtc: expires,
                    phoneNumber: normalized,
                    purpose: purpose,
                    channel: channel);
                _db.PhoneVerifications.Add(pv);
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        // ارسال SMS (اگر بعداً Outbox/Worker داری، اینجا می‌تونی DomainEvent بلند کنی)
        var templateName = _opts.Template?.OtpTemplateName ?? "default_otp";
        var send = await _sender.SendCodeAsync(normalized, code, templateName);
        if (send.IsFailure)
        {
            _log.LogWarning(Events.Send, "SMS send failed. user={UserId}, phone={Phone}",
                user.Id, SensitiveDataMasker.MaskPhone(normalized));
            return Result.Failure(ErrorCodes.Common.OPERATION_FAILED, "Failed to send verification SMS.");
        }

        _log.LogInformation(Events.Send, "Verification code sent. user={UserId}, phone={Phone}",
            user.Id, SensitiveDataMasker.MaskPhone(normalized));
        return Result.Success();
    }

    public async Task<Result> VerifyCodeAsync(string userId, string code, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid) || uid == Guid.Empty)
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var user = await _users.FindByIdAsync(uid.ToString());
        if (user is null || user.IsDeleted)
            return Result.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

        // آخرین رکورد معتبر را بگیر
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

        // مقایسه‌ی امن
        var ok = false;
        try
        {
            ok = BCrypt.Net.BCrypt.Verify(code, pv.CodeHash);
        }
        finally
        {
            // anti-enumeration: تأخیر کوچک ثابت
            await Task.Delay(150, ct);
        }

        if (!ok)
        {
            pv.TryIncrementAttempts(_opts.MaxAttempts);
            await _db.SaveChangesAsync(ct);
            return Result.Failure(ErrorCodes.Common.OPERATION_FAILED, "Invalid code.");
        }

        // موفق: تأیید و به‌روزرسانی
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

    // ----------------- Utils -----------------

    private bool IsPhoneAllowed(string phone)
    {
        var p = Normalization.NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(p)) return false;

        var pattern = _opts.Security.AllowedPhonePattern;
        if (!string.IsNullOrWhiteSpace(pattern))
            return Regex.IsMatch(p, pattern);

        // fallback عمومی
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
}
