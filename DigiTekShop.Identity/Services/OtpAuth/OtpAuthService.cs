using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;
using DigiTekShop.Contracts.Abstractions.Identity.Device;
using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Options.Phone;
using DigiTekShop.SharedKernel.Enums.Auth;
using DigiTekShop.SharedKernel.Utilities.Security;
using DigiTekShop.SharedKernel.Utilities.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DigiTekShop.Identity.Services.OtpAuth;

public sealed class OtpAuthService : IAuthService
{
    private readonly ICurrentClient _client;
    private readonly UserManager<User> _users;
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IRateLimiter _rateLimiter;
    private readonly IDeviceRegistry _devices;
    private readonly ITokenService _tokens;
    private readonly ILoginAttemptService _attempts;
    private readonly IPhoneSender _sms;
    private readonly PhoneVerificationOptions _phoneOpts;
    private readonly ILogger<OtpAuthService> _log;

    public OtpAuthService(
        ICurrentClient client,
        UserManager<User> users,
        DigiTekShopIdentityDbContext db,
        IRateLimiter rateLimiter,
        IDeviceRegistry devices,
        ITokenService tokens,
        ILoginAttemptService attempts,
        IPhoneSender sms,
        IOptions<PhoneVerificationOptions> phoneOpts,
        ILogger<OtpAuthService> log)
    {
        _client = client;
        _users = users;
        _db = db;
        _rateLimiter = rateLimiter;
        _devices = devices;
        _tokens = tokens;
        _attempts = attempts;
        _sms = sms;
        _phoneOpts = phoneOpts.Value;
        _log = log;
    }

    public async Task<Result> SendOtpAsync(SendOtpRequestDto dto, CancellationToken ct)
    {
        var ip = _client.IpAddress ?? "n/a";
        var ua = _client.UserAgent ?? "n/a";

        // Normalize & validate phone
        var phone = Normalization.NormalizePhoneIranE164(dto.Phone);
        if (string.IsNullOrWhiteSpace(phone))
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        if (!Regex.IsMatch(phone, _phoneOpts.Security.AllowedPhonePattern))
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        // Rate-limit (phone + ip windowed)
        var ipKey = Hashing.Sha256Base64Url(ip);
        var win = TimeSpan.FromSeconds(_phoneOpts.WindowSeconds);
        var rlKey = $"otp:send:{phone}:{ipKey}";
        if (!await _rateLimiter.ShouldAllowAsync(rlKey, _phoneOpts.MaxSendPerWindow, win, ct))
            return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);

        // Optional: tighter counters (hour/day/month/ip)
        var tight = await EnforceCountersAsync(phone, ipKey, _phoneOpts.Security, ct);
        if (tight.IsFailure) return tight;

        // Find user (ignore soft delete)
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedPhoneNumber == phone && !u.IsDeleted, ct);

        // Resend cooldown: find last unexpired verification (by phone or user)
        PhoneVerification? lastPv = null;
        if (user is not null)
        {
            lastPv = await _db.PhoneVerifications
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(x => x.UserId == user.Id && !x.IsVerified, ct);
        }

        if (lastPv is null)
        {
            // fallback by phone (in rare case user just got created)
            lastPv = await _db.PhoneVerifications
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phone && !x.IsVerified, ct);
        }

        if (lastPv is not null && _phoneOpts.AllowResendCode)
        {
            var nextAllowed = lastPv.CreatedAtUtc.Add(_phoneOpts.ResendCooldown);
            if (DateTime.UtcNow < nextAllowed)
                return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
        }

        // Create user if needed
        if (user is null)
        {
            user = User.CreateFromPhone(phone, customerId: null, phoneConfirmed: false);
            user.UserName = phone;
            var create = await _users.CreateAsync(user);
            if (!create.Succeeded)
            {
                _log.LogWarning("Create user failed for phone={Phone}", phone);
                return Result.Failure(ErrorCodes.Common.OPERATION_FAILED);
            }
        }

        // Generate & hash code
        var code = GenerateNumericCode(_phoneOpts.CodeLength);
        var codeHash = HashOtp(code, _phoneOpts.CodeHashSecret);
        var now = DateTime.UtcNow;
        var expires = now.Add(_phoneOpts.CodeValidity);

        // Upsert verification row
        var pv = await _db.PhoneVerifications
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == user.Id && !x.IsVerified && x.ExpiresAtUtc > now, ct);

        if (pv is null)
        {
            pv = PhoneVerification.CreateForUser(
                userId: user.Id,
                codeHash: codeHash,
                createdAtUtc: now,
                expiresAtUtc: expires,
                phoneNumber: phone,
                ipAddress: _client.IpAddress,
                userAgent: _client.UserAgent
            );
            _db.PhoneVerifications.Add(pv);
        }
        else
        {
            pv.ResetCode(
                newHash: codeHash,
                newCreatedAtUtc: now,
                newExpiresAtUtc: expires,
                phoneNumber: phone,
                ipAddress: _client.IpAddress,
                userAgent: _client.UserAgent
            );
        }

        await _db.SaveChangesAsync(ct);

        // SMS template
        var template = _phoneOpts.Template.UseEnglishTemplate
            ? _phoneOpts.Template.CodeTemplateEnglish
            : _phoneOpts.Template.CodeTemplate;

        var company = _phoneOpts.Template.CompanyName ?? string.Empty;
        var text = string.Format(template, code, company);

        // Trim to MaxSmsLength if needed
        if (!string.IsNullOrEmpty(text) && text.Length > _phoneOpts.Template.MaxSmsLength)
            text = text[.._phoneOpts.Template.MaxSmsLength];

        await _sms.SendCodeAsync(phone, text, template,ct);

        await RecordAttempt(user.Id, LoginStatus.OtpSent, dto.Phone, ip, ua, ct);
        return Result.Success();
    }

    public async Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpRequestDto dto, CancellationToken ct)
    {
        var ip = _client.IpAddress ?? "n/a";
        var ua = _client.UserAgent ?? "n/a";
        var deviceId = _client.DeviceId ?? dto.DeviceId ?? "unknown";

        var phone = Normalization.NormalizePhoneIranE164(dto.Phone);
        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, _phoneOpts.Security.AllowedPhonePattern))
            return Result<LoginResponseDto>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        // Find or create user
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedPhoneNumber == phone && !u.IsDeleted, ct);

        var isNew = false;
        if (user is null)
        {
            user = User.CreateFromPhone(phone, customerId: null, phoneConfirmed: false);
            user.UserName = phone;
            var create = await _users.CreateAsync(user);
            if (!create.Succeeded)
            {
                await RecordAttempt(null, LoginStatus.Failed, dto.Phone, ip, ua, ct);
                return Result<LoginResponseDto>.Failure(ErrorCodes.Common.OPERATION_FAILED);
            }
            isNew = true;
        }

        if (user.IsLocked)
        {
            await RecordAttempt(user.Id, LoginStatus.LockedOut, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        // Latest active OTP
        var pv = await _db.PhoneVerifications
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == user.Id && !x.IsVerified && x.ExpiresAtUtc > DateTime.UtcNow, ct);

        if (pv is null)
        {
            await UniformDelayAsync(ct);
            await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        // Attempts check
        if (!pv.IsValid(DateTime.UtcNow, _phoneOpts.MaxAttempts))
        {
            await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
        }

        // Verify code (constant-time)
        var codeHash = HashOtp(dto.Code, _phoneOpts.CodeHashSecret);
        if (!FixedTimeEquals(codeHash, pv.CodeHash))
        {
            pv.TryIncrementAttempts(_phoneOpts.MaxAttempts);
            await _db.SaveChangesAsync(ct);
            await UniformDelayAsync(ct);
            await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        // Success: confirm phone, record login, register device, trust if requested
        pv.MarkAsVerified(DateTime.UtcNow);
        user.SetPhoneNumber(phone, confirmed: true);
        user.RecordLogin(DateTime.UtcNow);
        await _db.SaveChangesAsync(ct);

        await _devices.UpsertAsync(user.Id, deviceId, ua, ip, ct);
        if (dto.RememberDevice && _phoneOpts.TrustDeviceDays > 0)
        {
            await _devices.TrustAsync(user.Id, deviceId, TimeSpan.FromDays(_phoneOpts.TrustDeviceDays), ct);
        }

        var issued = await _tokens.IssueAsync(user.Id, ct);
        if (issued.IsFailure)
        {
            await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(issued.Errors!, issued.ErrorCode!);
        }

        await RecordAttempt(user.Id, LoginStatus.Success, dto.Phone, ip, ua, ct);

        var v = issued.Value;
        var resp = new LoginResponseDto
        {
            UserId = user.Id,
            IsNewUser = isNew,
            AccessToken = v.AccessToken,
            AccessTokenExpiresAtUtc = v.ExpiresAtUtc,
            RefreshToken = v.RefreshToken,
            // TODO: وقتی TokenService تاریخ انقضای رفرش را برگرداند این‌جا ست کن
            RefreshTokenExpiresAtUtc = default
        };

        return Result<LoginResponseDto>.Success(resp);
    }

    public Task<Result<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest dto, CancellationToken ct)
        => _tokens.RefreshAsync(dto, ct);

    public Task<Result> LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct)
        => _tokens.RevokeAsync(refreshToken, userId, ct);

    public Task<Result> LogoutAllAsync(Guid userId, string? reason, CancellationToken ct)
        => _tokens.RevokeAllAsync(userId, ct);

    #region Helpers

    private async Task<Result> EnforceCountersAsync(string phone, string ipKey, PhoneSecurityOptions sec, CancellationToken ct)
    {
        try
        {
            var okHourPhone = await _rateLimiter.ShouldAllowAsync($"otp:hour:{phone}", sec.MaxRequestsPerHour, TimeSpan.FromHours(1), ct);
            var okDayPhone = await _rateLimiter.ShouldAllowAsync($"otp:day:{phone}", sec.MaxRequestsPerDay, TimeSpan.FromDays(1), ct);
            var okMonthPh = await _rateLimiter.ShouldAllowAsync($"otp:month:{phone}", sec.MaxRequestsPerMonth, TimeSpan.FromDays(30), ct);

            if (!(okHourPhone && okDayPhone && okMonthPh))
                return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);

            if (sec.IpRestrictionEnabled)
            {
                var okHourIp = await _rateLimiter.ShouldAllowAsync($"otp:hourip:{ipKey}", sec.MaxRequestsPerIpPerHour, TimeSpan.FromHours(1), ct);
                if (!okHourIp)
                    return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
            }

            return Result.Success();
        }
        catch
        {
            // fail-open
            return Result.Success();
        }
    }

    private async Task RecordAttempt(Guid? userId, LoginStatus status, string? login, string ip, string ua, CancellationToken ct)
    {
        try
        {
            await _attempts.RecordLoginAttemptAsync(userId, status, ip, ua, login, ct);
        }
        catch { /* no-op */ }
    }

    private static string GenerateNumericCode(int len)
    {
        Span<byte> bytes = stackalloc byte[len];
        RandomNumberGenerator.Fill(bytes);
        var sb = new StringBuilder(len);
        foreach (var b in bytes) sb.Append((b % 10).ToString());
        return sb.ToString();
    }

    private static string HashOtp(string code, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private static Task UniformDelayAsync(CancellationToken ct)
        => Task.Delay(TimeSpan.FromMilliseconds(300 + RandomNumberGenerator.GetInt32(200)), ct);

    #endregion
}
