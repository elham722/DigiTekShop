using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;


namespace DigiTekShop.Identity.Services.Register;

public sealed class PhoneVerificationService : IPhoneVerificationService
{
    private readonly UserManager<User> _userManager;
    private readonly IPhoneSender _phoneSender;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly PhoneVerificationSettings _settings;
    private readonly IRateLimiter _rateLimiter;
    
    private readonly ILogger<PhoneVerificationService> _logger;

    public PhoneVerificationService(
        UserManager<User> userManager,
        IPhoneSender phoneSender,
        DigiTekShopIdentityDbContext context,
        IOptions<PhoneVerificationSettings> settings,
        IRateLimiter rateLimiter,
        ILogger<PhoneVerificationService> logger)
    {
        _userManager = userManager;
        _phoneSender = phoneSender;
        _context = context;
        _settings = settings.Value;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

  

    public async Task<Result> SendVerificationCodeAsync(Guid userId, string phoneNumber, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted) return Result.Failure("User not found or inactive.");

        return await SendVerificationCodeCoreAsync(user, phoneNumber, ct);
    }

    public async Task<Result> VerifyCodeAsync(string userId, string code, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted) return Result.Failure("User not found or inactive.");

        return await VerifyCodeCoreAsync(user, code, ct);
    }

    public async Task<bool> CanResendCodeAsync(Guid userId, CancellationToken ct = default)
    {
        var last = await _context.PhoneVerifications
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return last == null || DateTime.UtcNow >= last.CreatedAt.Add(_settings.ResendCooldown);
    }

   

    private async Task<Result> SendVerificationCodeCoreAsync(User user, string phoneNumber, CancellationToken ct)
    {
        if (!_settings.RequirePhoneConfirmation || user.PhoneNumberConfirmed)
            return Result.Success();

        Guard.AgainstInvalidFormat(phoneNumber, _settings.Security.AllowedPhonePattern, nameof(phoneNumber));

        
        if (_settings.Security.RequireUniquePhoneNumbers && !string.Equals(user.PhoneNumber, phoneNumber, StringComparison.Ordinal))
            return Result.Failure("Phone number does not match the registered number.");

        // Rate limit
        var rateLimitKey = $"phone_verification:{user.Id}";
        var isAllowed = await _rateLimiter.ShouldAllowAsync(
            rateLimitKey,
            _settings.Security.MaxRequestsPerHour,
            TimeSpan.FromHours(1));

        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for phone verification user {UserId}", user.Id);
            return Result.Failure("Too many verification requests. Please try again later.");
        }

        if (_settings.AllowResendCode && !await CanResendCodeAsync(user.Id, ct))
            return Result.Failure($"Please wait {_settings.ResendCooldown} minutes before resend.");

        var code = GenerateCode(_settings.CodeLength);
        var hash = BCrypt.Net.BCrypt.HashPassword(code);
        var expires = DateTime.UtcNow.Add(_settings.CodeValidity);

        await GetOrCreateAndPersistVerificationAsync(user.Id, hash, expires, phoneNumber, ct);

        var templateName = _settings.Template.OtpTemplateName;
        var sendResult = await _phoneSender.SendCodeAsync(phoneNumber, code, templateName);

        if (sendResult.IsFailure) return Result.Failure("Failed to send SMS.");

        _logger.LogInformation("Verification code sent to {Phone} for user {UserId}", phoneNumber, user.Id);
        return Result.Success();
    }

    private async Task<Result> VerifyCodeCoreAsync(User user, string enteredCode, CancellationToken ct)
    {
        var verification = await _context.PhoneVerifications
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (verification == null) return Result.Failure("No code found.");
        if (verification.IsExpired()) return Result.Failure("Code expired.");
        if (verification.Attempts >= _settings.MaxAttempts) return Result.Failure("Too many attempts.");

        
        verification.IncrementAttempts(_settings.MaxAttempts);
        await _context.SaveChangesAsync(ct);

        if (!BCrypt.Net.BCrypt.Verify(enteredCode, verification.CodeHash))
            return Result.Failure("Invalid code.");

       
        verification.MarkAsVerified();
        user.PhoneNumberConfirmed = true;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return Result.Failure(string.Join(", ", update.Errors.Select(e => e.Description)));

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Phone {Phone} verified for user {UserId}", user.PhoneNumber, user.Id);
        return Result.Success();
    }


    private static string GenerateCode(int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        Span<char> chars = stackalloc char[length];
        for (int i = 0; i < length; i++)
            chars[i] = (char)('0' + bytes[i] % 10);
        return new string(chars);
    }

   
    private async Task GetOrCreateAndPersistVerificationAsync(Guid userId, string hash, DateTime expires, string? phoneNumber, CancellationToken ct)
    {
        var existing = await _context.PhoneVerifications
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            existing.ResetCode(hash, expires);
        }
        else
        {
            var pv = PhoneVerification.Create(userId, hash, expires, phoneNumber);
            _context.PhoneVerifications.Add(pv);
        }

        await _context.SaveChangesAsync(ct);
    }

}
