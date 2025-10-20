using DigiTekShop.Contracts.Options.Email;
using DigiTekShop.Contracts.Options.Phone;
using DigiTekShop.SharedKernel.Enums.Auth;
using DigiTekShop.SharedKernel.Utilities.Security;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Services.Register;

public sealed class RegistrationService : IRegistrationService
{
    private static class Events
    {
        public static readonly EventId Register = new(44001, nameof(RegisterAsync));
        public static readonly EventId Rate = new(44002, "RateLimit");
        public static readonly EventId Exists = new(44003, "UserExists");
        public static readonly EventId Create = new(44004, "CreateUser");
        public static readonly EventId Outbox = new(44005, "OutboxSave");
    }

    private readonly ICurrentClient _client;
    private readonly UserManager<User> _userManager;
    private readonly IRateLimiter _rateLimiter;
    private readonly PhoneVerificationOptions _phoneSettings;
    private readonly EmailConfirmationOptions _emailSettings;
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly ILogger<RegistrationService> _log;
    private readonly IDomainEventSink _events;
    private readonly ICorrelationContext _corr;

    public RegistrationService(
        ICurrentClient client,
        UserManager<User> userManager,
        IRateLimiter rateLimiter,
        IOptions<PhoneVerificationOptions> phoneOptions,
        IOptions<EmailConfirmationOptions> emailOptions,
        DigiTekShopIdentityDbContext db,
        ILogger<RegistrationService> log,
        IDomainEventSink events,
        ICorrelationContext corr)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _phoneSettings = phoneOptions?.Value ?? new PhoneVerificationOptions();
        _emailSettings = emailOptions?.Value ?? new EmailConfirmationOptions { RequireEmailConfirmation = true };
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _corr = corr ?? throw new ArgumentNullException(nameof(corr));
    }

    public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Result<RegisterResponseDto>.Failure(new[] { "email/password: required" }, ErrorCodes.Common.VALIDATION_FAILED);

            if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                return Result<RegisterResponseDto>.Failure(new[] { "confirmPassword: does not match" }, ErrorCodes.Common.VALIDATION_FAILED);

            var emailNorm = Normalization.Normalize(request.Email)!; 
            var phoneNorm = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : Normalization.NormalizePhone(request.PhoneNumber);

            var ip = _client.IpAddress ?? "n/a";
            var ipKey = Hashing.Sha256Base64Url(ip);
            var okRate = await CheckRateLimitsAsync(emailNorm, ipKey, ct);
            if (okRate.IsFailure) return Result<RegisterResponseDto>.Failure(okRate.Errors, okRate.ErrorCode);

            var normalizedEmailForLookup = _userManager.NormalizeEmail(emailNorm) ?? emailNorm.ToUpperInvariant();
            var exists = await _db.Users.AsNoTracking().IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalizedEmailForLookup, ct);

            if (exists)
            {
                _log.LogInformation(Events.Exists, "Registration exists/idempotent for {Email}", SensitiveDataMasker.MaskEmail(emailNorm));
                
                return Result<RegisterResponseDto>.Failure(new[] { "email: already registered" }, ErrorCodes.Identity.USER_EXISTS);
            }

            
            var user = User.Create(emailNorm, emailNorm);
            user.UserName = emailNorm;
            user.Email = emailNorm;
            if (!string.IsNullOrWhiteSpace(phoneNorm)) user.PhoneNumber = phoneNorm;

            var create = await _userManager.CreateAsync(user, request.Password);
            if (!create.Succeeded)
            {
                var mapped = MapIdentityErrors(create.Errors);
                _log.LogWarning(Events.Create, "User creation failed for {Email}. Errors: {Errors}", SensitiveDataMasker.MaskEmail(emailNorm), string.Join(" | ", mapped));
                
                var code = create.Errors.Any(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    ? ErrorCodes.Identity.USER_EXISTS
                    : ErrorCodes.Common.OPERATION_FAILED;

                return Result<RegisterResponseDto>.Failure(mapped, code);
            }

            
            var corrId = _corr.GetCorrelationId();
            _events.Raise(new UserRegisteredDomainEvent(
                UserId: user.Id,
                Email: user.Email!,
                PhoneNumber: user.PhoneNumber,
                FullName: null,
                OccurredOn: DateTimeOffset.UtcNow,
                CorrelationId: corrId));

            await _db.SaveChangesAsync(ct);
            _log.LogInformation(Events.Outbox, "Outbox persisted for user {UserId} with CorrelationId {CorrId}", user.Id, corrId);

            var requireEmail = _emailSettings.RequireEmailConfirmation && !user.EmailConfirmed;
            var requirePhone = _phoneSettings.RequirePhoneConfirmation && !string.IsNullOrWhiteSpace(user.PhoneNumber) && !user.PhoneNumberConfirmed;

            var next = RegisterNextStep.None;
            if (requireEmail) next = RegisterNextStep.ConfirmEmail;
            else if (requirePhone) next = RegisterNextStep.VerifyPhone;

            var resp = new RegisterResponseDto(
                UserId: user.Id,
                RequireEmailConfirmation: requireEmail,
                EmailSent: requireEmail,    
                RequirePhoneConfirmation: requirePhone,
                PhoneCodeSent: requirePhone,  
                NextStep: next
            );

            _log.LogInformation(Events.Register, "User {UserId} registered. DevId={Dev} UA={UA} IP={IP}",
                user.Id, _client.DeviceId, _client.UserAgent, ip);

            return Result<RegisterResponseDto>.Success(resp);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unexpected error during registration for {Email}", SensitiveDataMasker.MaskEmail(request.Email));
            return Result<RegisterResponseDto>.Failure(
                new[] { "general: unexpected error" },
                ErrorCodes.Common.OPERATION_FAILED);
        }
    }

    #region Helpers

    private async Task<Result> CheckRateLimitsAsync(string emailNorm, string ipHashKey, CancellationToken ct)
    {
        try
        {
            var tasks = new[]
            {
                _rateLimiter.ShouldAllowAsync($"reg:email:{emailNorm}", 5,  TimeSpan.FromMinutes(30), ct),
                _rateLimiter.ShouldAllowAsync($"reg:ip:{ipHashKey}",     20, TimeSpan.FromMinutes(10), ct),
                _rateLimiter.ShouldAllowAsync($"reg:emailip:{emailNorm}:{ipHashKey}", 3, TimeSpan.FromMinutes(10), ct)
            };

            var res = await Task.WhenAll(tasks);
            if (res.Any(allowed => !allowed))
            {
                _log.LogWarning(Events.Rate, "Registration rate-limited for {Email}", SensitiveDataMasker.MaskEmail(emailNorm));
                return Result.Failure(new[] { "Too many registration attempts. Please try again later." }, ErrorCodes.Common.RATE_LIMIT_EXCEEDED);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "RateLimiter failure (fail-open) on registration");
            return Result.Success();
        }
    }

    private static IEnumerable<string> MapIdentityErrors(IEnumerable<IdentityError> errors)
    {
        foreach (var e in errors)
        {
            var field = e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "password"
                      : e.Code.Contains("UserName", StringComparison.OrdinalIgnoreCase) ? "email"
                      : "general";
            yield return $"{field}: {e.Description}";
        }
    }

    #endregion
}
