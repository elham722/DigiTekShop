#nullable enable
using DigiTekShop.Identity.Events.PhoneVerification;
using DigiTekShop.SharedKernel.Exceptions.Common;
using DigiTekShop.SharedKernel.Http;

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
    private readonly PhoneVerificationOptions _phoneOpts;
    private readonly ILogger<OtpAuthService> _log;
    private readonly IDomainEventSink _sink;
    private readonly ICorrelationContext _corr;
    private readonly IEncryptionService _crypto;

    public OtpAuthService(
        ICurrentClient client,
        UserManager<User> users,
        DigiTekShopIdentityDbContext db,
        IRateLimiter rateLimiter,
        IDeviceRegistry devices,
        ITokenService tokens,
        ILoginAttemptService attempts,
        IOptions<PhoneVerificationOptions> phoneOpts,
        IDomainEventSink sink,
        ICorrelationContext corr,
        IEncryptionService crypto,
        ILogger<OtpAuthService> log)
    {
        _client = client;
        _users = users;
        _db = db;
        _rateLimiter = rateLimiter;
        _devices = devices;
        _tokens = tokens;
        _attempts = attempts;
        _phoneOpts = phoneOpts.Value;
        _sink = sink;
        _corr = corr;
        _crypto = crypto;
        _log = log;
    }

    // --------------------------------------------------------------------
    // Send OTP
    // --------------------------------------------------------------------
    public async Task<Result> SendOtpAsync(SendOtpRequestDto dto, CancellationToken ct)
    {
        var ip = _client.IpAddress ?? "n/a";
        var ua = _client.UserAgent ?? "n/a";
        var deviceId = _client.DeviceId ?? "unknown";

        // 1) Normalize & validate
        var phone = Normalization.NormalizePhoneIranE164(dto.Phone);
        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, _phoneOpts.Security.AllowedPhonePattern))
            return Result.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        // 2) Rate-limit (phone + ip windowed) → throw RateLimitedException تا Handler هدرها را ست کند
        var ipKey = Hashing.Sha256Base64Url(ip);
        var win = TimeSpan.FromSeconds(Math.Max(5, _phoneOpts.WindowSeconds));
        var rlKey = $"otp:send:{phone}:{ipKey}";
        var d = await _rateLimiter.ShouldAllowAsync(rlKey, Math.Max(1, _phoneOpts.MaxSendPerWindow), win, ct);
        if (!d.Allowed)
            return Result.Failure(ErrorCodes.Otp.OTP_SEND_RATE_LIMITED)
                .WithRateLimit(d, policy: "OtpSendPolicy", key: rlKey, reason: ErrorCodes.Otp.OTP_SEND_RATE_LIMITED);

        // 3) Optional strict counters
        var tight = await EnforceCountersAsync(phone, ipKey, _phoneOpts.Security, ct);
        if (tight.IsFailure)
        {
            var fakeDecision = new RateLimitDecision(
                Allowed: false,
                Count: 1,
                Limit: _phoneOpts.Security.MaxRequestsPerHour,
                Window: TimeSpan.FromHours(1),
                ResetAt: DateTimeOffset.UtcNow.AddHours(1),
                Ttl: null);
            
            return Result.Failure(ErrorCodes.Otp.OTP_SEND_RATE_LIMITED)
                .WithRateLimit(fakeDecision, "OtpSendPolicy", $"otp:hour:{phone}", ErrorCodes.Otp.OTP_SEND_RATE_LIMITED);
        }

        // 4) Find/Create user
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedPhoneNumber == phone && !u.IsDeleted, ct);

        if (user is null)
        {
            user = User.CreateFromPhone(phone, customerId: null, phoneConfirmed: false);
            user.UserName = phone;
            var create = await _users.CreateAsync(user);
            if (!create.Succeeded)
            {
                _log.LogWarning("Create user failed for phone={Phone}. Errors: {Errors}",
                    phone, string.Join(", ", create.Errors.Select(e => $"{e.Code}:{e.Description}")));
                return Result.Failure(ErrorCodes.Common.OPERATION_FAILED);
            }
        }

        // 5) Resend cooldown
        var lastPv = await FindLastActiveUnverifiedPVAsync(user.Id, phone, ct);
        if (lastPv is not null)
        {
            if (!_phoneOpts.AllowResendCode && lastPv.ExpiresAtUtc > DateTime.UtcNow)
            {
                // تا زمان انقضای کد قبلی اجازه‌ی ارسال مجدد نده
                var fakeDecision = BuildCooldownRateLimitDecision(lastPv.ExpiresAtUtc, policy: "OtpSendPolicy", reason: ErrorCodes.Otp.OTP_SEND_RATE_LIMITED);
                return Result.Failure(ErrorCodes.Otp.OTP_SEND_RATE_LIMITED)
                    .WithRateLimit(fakeDecision, "OtpSendPolicy", "cooldown", ErrorCodes.Otp.OTP_SEND_RATE_LIMITED);
            }
            if (_phoneOpts.AllowResendCode)
            {
                var nextAllowed = lastPv.CreatedAtUtc.Add(_phoneOpts.ResendCooldown);
                if (DateTime.UtcNow < nextAllowed)
                {
                    var fakeDecision = BuildCooldownRateLimitDecision(nextAllowed, policy: "OtpSendPolicy", reason: ErrorCodes.Otp.OTP_SEND_RATE_LIMITED);
                    return Result.Failure(ErrorCodes.Otp.OTP_SEND_RATE_LIMITED)
                        .WithRateLimit(fakeDecision, "OtpSendPolicy", "cooldown", ErrorCodes.Otp.OTP_SEND_RATE_LIMITED);
                }
            }
        }

        // 6) Generate/Upsert PV
        var code = GenerateNumericCode(_phoneOpts.CodeLength);
        var codeHash = HashOtp(code, _phoneOpts.CodeHashSecret);
        var protectedCode = _crypto.Encrypt(code, SharedKernel.Enums.Security.CryptoPurpose.TotpSecret);
        var now = DateTime.UtcNow;
        var expires = now.Add(_phoneOpts.CodeValidity);

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
                userAgent: _client.UserAgent,
                deviceId: deviceId,
                codeHashAlgo: "HMACSHA256",
                secretVersion: 1,
                encryptedCodeProtected: protectedCode
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
                userAgent: _client.UserAgent,
                deviceId: deviceId,
                codeHashAlgo: "HMACSHA256",
                secretVersion: 1,
                encryptedCodeProtected: protectedCode
            );
        }

        // 7) Raise domain event (Outbox)
        _sink.Raise(new PhoneVerificationIssuedDomainEvent(
            user.Id, phone, pv.Id, DateTimeOffset.UtcNow, _corr.GetCorrelationId()));

        await _db.SaveChangesAsync(ct);
        await RecordAttempt(user.Id, LoginStatus.OtpSent, dto.Phone, ip, ua, ct);
        return Result.Success();
    }

    // --------------------------------------------------------------------
    // Verify OTP
    // --------------------------------------------------------------------
    public async Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpRequestDto dto, CancellationToken ct)
    {
        var ip = _client.IpAddress ?? "n/a";
        var ua = _client.UserAgent ?? "n/a";
        var deviceId = _client.DeviceId ?? "unknown";

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

        // آخرین PV فعال
        var pv = await _db.PhoneVerifications
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == user.Id && !x.IsVerified && x.ExpiresAtUtc > DateTime.UtcNow, ct)
            ?? await _db.PhoneVerifications
                 .OrderByDescending(x => x.CreatedAtUtc)
                 .FirstOrDefaultAsync(x => x.PhoneNumber == phone && !x.IsVerified && x.ExpiresAtUtc > DateTime.UtcNow, ct);

        if (pv is null)
        {
            await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Otp.OTP_EXPIRED);
        }

        _log.LogInformation("[VerifyOTP] PV id={PV}, expires={Exp:o}, attempts={Att}/{Max}",
            pv.Id, pv.ExpiresAtUtc, pv.Attempts, _phoneOpts.MaxAttempts);

        // محدودیت تلاش (business-level)
        if (!pv.IsValid(DateTime.UtcNow, _phoneOpts.MaxAttempts))
        {
            // 429 به‌جای 400 برای UX بهتر
            var fakeDecision = BuildCooldownRateLimitDecision(pv.ExpiresAtUtc, policy: "OtpVerifyPolicy", reason: ErrorCodes.Otp.OTP_VERIFY_RATE_LIMITED);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Otp.OTP_VERIFY_RATE_LIMITED)
                .WithRateLimit(fakeDecision, "OtpVerifyPolicy", "cooldown", ErrorCodes.Otp.OTP_VERIFY_RATE_LIMITED);
        }

        // (اختیاری) ریت‌لیمیت شبکه‌ای برای Verify (مستقل از attempts)
        // اگر گزینه‌ای برایش داری، فعال کن:
        if (_phoneOpts.MaxVerifyPerWindow > 0)
        {
            var win = TimeSpan.FromSeconds(Math.Max(5, _phoneOpts.VerifyWindowSeconds));
            var key = $"otp:verify:{pv.Id:N}";
            var d = await _rateLimiter.ShouldAllowAsync(key, _phoneOpts.MaxVerifyPerWindow, win, ct);
            if (!d.Allowed)
                return Result<LoginResponseDto>.Failure(ErrorCodes.Otp.OTP_VERIFY_RATE_LIMITED)
                    .WithRateLimit(d, "OtpVerifyPolicy", key, ErrorCodes.Otp.OTP_VERIFY_RATE_LIMITED);
        }

        // Verify code (constant-time)
        var codeHash = HashOtp(dto.Code, _phoneOpts.CodeHashSecret);
        if (string.IsNullOrEmpty(pv.CodeHash) || !FixedTimeEquals(codeHash, pv.CodeHash))
        {
            pv.TryIncrementAttempts(_phoneOpts.MaxAttempts);
            await _db.SaveChangesAsync(ct);
            await UniformDelayAsync(ct);
            await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Otp.OTP_INVALID);
        }

        // (اختیاری) enforce device binding
        // if (!string.Equals(pv.DeviceId, deviceId, StringComparison.Ordinal))
        //     return Result<LoginResponseDto>.Failure(ErrorCodes.Common.VALIDATION_FAILED);

        var now = DateTime.UtcNow;
        pv.MarkAsVerified(now);
        user.SetPhoneNumber(phone, confirmed: true);
        user.RecordLogin(now);

        var shouldRaiseUserRegistered =
            user.PhoneNumberConfirmed == true &&
            user.CustomerId is null;

        if (shouldRaiseUserRegistered)
        {
            _sink.Raise(new UserRegisteredDomainEvent(
                UserId: user.Id,
                Email: user.Email ?? string.Empty,
                FullName: !string.IsNullOrWhiteSpace(user.NormalizedEmail) ? user.NormalizedEmail :
                           !string.IsNullOrWhiteSpace(user.Email) ? user.Email :
                           !string.IsNullOrWhiteSpace(user.PhoneNumber) ? user.PhoneNumber :
                           $"user-{user.Id:N}",
                PhoneNumber: user.PhoneNumber ?? string.Empty,
                OccurredOn: DateTimeOffset.UtcNow,
                CorrelationId: _corr.GetCorrelationId()
            ));
        }

        await _db.SaveChangesAsync(ct);

        await _devices.UpsertAsync(user.Id, deviceId, ua, ip, ct);
        if (_phoneOpts.TrustDeviceDays > 0)
            await _devices.TrustAsync(user.Id, deviceId, TimeSpan.FromDays(_phoneOpts.TrustDeviceDays), ct);

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
            RefreshTokenExpiresAtUtc = default
        };
        return Result<LoginResponseDto>.Success(resp);
    }

    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------
    private async Task<PhoneVerification?> FindLastActiveUnverifiedPVAsync(Guid userId, string phone, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var pv = await _db.PhoneVerifications
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsVerified && x.ExpiresAtUtc > now, ct);

        if (pv is not null) return pv;

        pv = await _db.PhoneVerifications
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.PhoneNumber == phone && !x.IsVerified && x.ExpiresAtUtc > now, ct);

        return pv;
    }

    private static RateLimitedException BuildCooldownRateLimit(DateTime expiresAtUtc, string policy, string reason)
    {
        var expUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
        var nowUtc = DateTime.UtcNow;

        var window = expUtc > nowUtc ? (expUtc - nowUtc) : TimeSpan.FromSeconds(1);
        var resetAt = new DateTimeOffset(expUtc, TimeSpan.Zero); 

        return RateLimitedException.FromRaw(
            count: 0,
            limit: 1,
            window: window,
            resetAt: resetAt,
            policy: policy,
            key: "cooldown",
            reason: reason
        );
    }

    private static RateLimitDecision BuildCooldownRateLimitDecision(DateTime expiresAtUtc, string policy, string reason)
    {
        var expUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
        var nowUtc = DateTime.UtcNow;

        var window = expUtc > nowUtc ? (expUtc - nowUtc) : TimeSpan.FromSeconds(1);
        var resetAt = new DateTimeOffset(expUtc, TimeSpan.Zero); 

        return new RateLimitDecision(
            Allowed: false,
            Count: 1,
            Limit: 1,
            Window: window,
            ResetAt: resetAt,
            Ttl: null
        );
    }


    private async Task<Result> EnforceCountersAsync(string phone, string ipKey, PhoneSecurityOptions sec, CancellationToken ct)
    {
        try
        {
            var hourPh = await _rateLimiter.ShouldAllowAsync($"otp:hour:{phone}", sec.MaxRequestsPerHour, TimeSpan.FromHours(1), ct);
            var dayPh = await _rateLimiter.ShouldAllowAsync($"otp:day:{phone}", sec.MaxRequestsPerDay, TimeSpan.FromDays(1), ct);
            var monthPh = await _rateLimiter.ShouldAllowAsync($"otp:month:{phone}", sec.MaxRequestsPerMonth, TimeSpan.FromDays(30), ct);

            if (!(hourPh.Allowed && dayPh.Allowed && monthPh.Allowed))
                return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);

            if (sec.IpRestrictionEnabled)
            {
                var hourIp = await _rateLimiter.ShouldAllowAsync($"otp:hourip:{ipKey}", sec.MaxRequestsPerIpPerHour, TimeSpan.FromHours(1), ct);
                if (!hourIp.Allowed)
                    return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
            }
            return Result.Success();
        }
        catch
        {
            // fail-open (Log کن)
            return Result.Success();
        }
    }

    private async Task RecordAttempt(Guid? userId, LoginStatus status, string? login, string ip, string ua, CancellationToken ct)
    {
        try { await _attempts.RecordLoginAttemptAsync(userId, status, ip, ua, login, ct); }
        catch { /* no-op */ }
    }

    private static string GenerateNumericCode(int len)
    {
        Span<char> chars = stackalloc char[len];
        int i = 0;
        Span<byte> buf = stackalloc byte[32];
        while (i < len)
        {
            RandomNumberGenerator.Fill(buf);
            for (int k = 0; k < buf.Length && i < len; k++)
            {
                var b = buf[k];
                if (b < 250) { chars[i++] = (char)('0' + (b % 10)); }
            }
        }
        return new string(chars);
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
}
