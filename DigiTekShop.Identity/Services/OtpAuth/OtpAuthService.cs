
using DigiTekShop.Identity.Events.PhoneVerification;

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

    public async Task<Result> SendOtpAsync(SendOtpRequestDto dto, CancellationToken ct)
    {
        var ip = _client.IpAddress ?? "n/a";
        var ua = _client.UserAgent ?? "n/a";
        var deviceId = _client.DeviceId ?? "unknown";

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

        // Optional strict counters (hour/day/month/ip)
        var tight = await EnforceCountersAsync(phone, ipKey, _phoneOpts.Security, ct);
        if (tight.IsFailure) return tight;

        // Find user (ignore soft delete)
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedPhoneNumber == phone && !u.IsDeleted, ct);

        // Resend cooldown: find last unverified (by user or phone)
        PhoneVerification? lastPv = null;
        if (user is not null)
        {
            lastPv = await _db.PhoneVerifications
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(x => x.UserId == user.Id && !x.IsVerified, ct);
        }
        if (lastPv is null)
        {
            lastPv = await _db.PhoneVerifications
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phone && !x.IsVerified, ct);
        }

        if (lastPv is not null)
        {
            if (!_phoneOpts.AllowResendCode)
            {
                if (lastPv.ExpiresAtUtc > DateTime.UtcNow)
                    return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
            }
            else
            {
                var nextAllowed = lastPv.CreatedAtUtc.Add(_phoneOpts.ResendCooldown);
                if (DateTime.UtcNow < nextAllowed)
                    return Result.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
            }
        }

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


        // Generate code + hash + protect
        var code = GenerateNumericCode(_phoneOpts.CodeLength);
        var codeHash = HashOtp(code, _phoneOpts.CodeHashSecret);
        var protectedCode = _crypto.Encrypt(code, DigiTekShop.SharedKernel.Enums.Security.CryptoPurpose.TotpSecret);

        var now = DateTime.UtcNow;
        var expires = now.Add(_phoneOpts.CodeValidity);

        // Upsert latest active PhoneVerification
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

        await _db.SaveChangesAsync(ct);

        // بلند کردن DomainEvent برای Outbox/Worker (ارسال SMS خارج از ریکوئست)
        _sink.Raise(new PhoneVerificationIssuedDomainEvent(
            user.Id, phone, pv.Id, DateTimeOffset.UtcNow, _corr.GetCorrelationId()));

        await RecordAttempt(user.Id, LoginStatus.OtpSent, dto.Phone, ip, ua, ct);
        return Result.Success();
    }

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

        // Latest active OTP by UserId
        var pv = await _db.PhoneVerifications
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == user.Id && !x.IsVerified && x.ExpiresAtUtc > DateTime.UtcNow, ct);

        _log.LogInformation("[VerifyOTP] user={UserId}, normalizedPhone={Phone}", user.Id, phone);
        // ⬇️ fallback by PhoneNumber (مثل SendOtp)
        if (pv is null)
        {
            _log.LogWarning("[VerifyOTP] No active PV found for user={UserId} or phone={Phone}", user.Id, phone);
            pv = await _db.PhoneVerifications
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phone && !x.IsVerified && x.ExpiresAtUtc > DateTime.UtcNow, ct);
        }

        if (pv is not null)
        {
            _log.LogInformation("[VerifyOTP] PV found: id={PV}, expires={ExpUtc:o}, attempts={Att}/{Max}",
                pv.Id, pv.ExpiresAtUtc, pv.Attempts, _phoneOpts.MaxAttempts);
        }
        if (!pv.IsValid(DateTime.UtcNow, _phoneOpts.MaxAttempts))
        {
            await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
        }

        // Verify code (constant-time)
        var codeHash = HashOtp(dto.Code, _phoneOpts.CodeHashSecret);
        _log.LogInformation("[VerifyOTP] comparing hashes: len(input)={InLen}, len(stored)={StLen}",
            codeHash?.Length ?? 0, pv.CodeHash?.Length ?? 0);
        if (!FixedTimeEquals(codeHash, pv.CodeHash))
        {
            pv.TryIncrementAttempts(_phoneOpts.MaxAttempts);
            await _db.SaveChangesAsync(ct);
            await UniformDelayAsync(ct);
            await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
            return Result<LoginResponseDto>.Failure(ErrorCodes.Identity.INVALID_CREDENTIALS);
        }

        // (اختیاری) enforce device binding
        // if (!string.Equals(pv.DeviceId, deviceId, StringComparison.Ordinal))
        // {
        //     await RecordAttempt(user.Id, LoginStatus.Failed, dto.Phone, ip, ua, ct);
        //     return Result<LoginResponseDto>.Failure(ErrorCodes.Common.VALIDATION_FAILED);
        // }


        pv.MarkAsVerified(DateTime.UtcNow);
        user.SetPhoneNumber(phone, confirmed: true);
        user.RecordLogin(DateTime.UtcNow);

        // بعد از اینکه pv.MarkAsVerified(...) و user.SetPhoneNumber(...confirmed: true) را انجام دادی:
        var shouldRaiseUserRegistered =
            (user.PhoneNumberConfirmed == true)   // الان تأیید است
            && isNew                              // کاربر تازه ساخته شده بود
            && user.CustomerId is null;           // هنوز Customer لینک نشده

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


        // ⚠️ خیلی مهم: Raise قبل از SaveChanges باشد تا Interceptor Outbox پیام را بنویسد
        await _db.SaveChangesAsync(ct);


        await _devices.UpsertAsync(user.Id, deviceId, ua, ip, ct);
        if (_phoneOpts.TrustDeviceDays > 0)
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
            RefreshTokenExpiresAtUtc = default
        };

        return Result<LoginResponseDto>.Success(resp);
    }

    // ----------------- Helpers -----------------

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
            // fail-open برای اینکه UX خراب نشه، اما لاگ سرور داشته باش
            return Result.Success();
        }
    }

    private async Task RecordAttempt(Guid? userId, LoginStatus status, string? login, string ip, string ua, CancellationToken ct)
    {
        try
        {
            await _attempts.RecordLoginAttemptAsync(userId, status, ip, ua, login, ct);
        }
        catch
        {
            // no-op
        }
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
        var ba = System.Text.Encoding.UTF8.GetBytes(a);
        var bb = System.Text.Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private static Task UniformDelayAsync(CancellationToken ct)
        => Task.Delay(TimeSpan.FromMilliseconds(300 + RandomNumberGenerator.GetInt32(200)), ct);
}
