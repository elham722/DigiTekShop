using DigiTekShop.Contracts.Abstractions.Telemetry;
using DigiTekShop.Identity.Events;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Identity.Services.Register;

public sealed class RegistrationService : IRegistrationService
{
    private const string RATE_LIMIT_TAG = "[RATE_LIMIT]";

    private readonly ICurrentClient _client;
    private readonly UserManager<User> _userManager;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RegistrationService> _logger;
    private readonly PhoneVerificationSettings _phoneSettings;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly EmailConfirmationSettings _emailSettings;
    private readonly IDomainEventSink _domainEvents;
    private readonly ICorrelationContext _correlationContext;

    public RegistrationService(
        ICurrentClient client,
        UserManager<User> userManager,
        IRateLimiter rateLimiter,
        IOptions<PhoneVerificationSettings> phoneOptions,
        IOptions<EmailConfirmationSettings> emailOptions,
        DigiTekShopIdentityDbContext context,
        ILogger<RegistrationService> logger,
        IDomainEventSink domainEvents,
        ICorrelationContext correlationContext)
    {
        _client = client;
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _phoneSettings = phoneOptions?.Value ?? throw new ArgumentNullException(nameof(phoneOptions));
        _emailSettings = emailOptions?.Value ?? new EmailConfirmationSettings { RequireEmailConfirmation = true };
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _domainEvents = domainEvents ?? throw new ArgumentNullException(nameof(domainEvents));
        _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
    }

    public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var req = Normalize(request);

            var deviceId = _client.DeviceId;
            var userAgent = _client.UserAgent;
            var ip = _client.IpAddress;

            
            var rl = await CheckRateLimitsAsync(req.Email, ip, ct);
            if (rl.IsFailure)
                return Result<RegisterResponseDto>.Failure(rl.Errors, rl.ErrorCode);

            
            var normalizedEmail = _userManager.NormalizeEmail(req.Email) ?? req.Email.ToUpperInvariant();
            var existsAny = await _context.Users.AsNoTracking().IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

            if (existsAny)
            {
                _logger.LogInformation("Registration idempotent/exists for {Email}", MaskEmail(req.Email));
                return Result<RegisterResponseDto>.Failure(
                    new[] { "email: Email already registered." },
                    ErrorCodes.Identity.USER_EXISTS);
            }
            var user = User.Create(req.Email, req.Email);
            user.UserName = req.Email; 
            user.Email = req.Email;   
            if (req.PhoneNumber is not null) user.PhoneNumber = req.PhoneNumber;

            var createResult = await _userManager.CreateAsync(user, req.Password);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors
                    .Select(e => $"{MapField(e)}: {e.Description}")
                    .ToList();
                _logger.LogWarning("User creation failed for {Email}. Errors: {Errors}",
                    MaskEmail(req.Email), string.Join(" | ", errors));
                return Result<RegisterResponseDto>.Failure(errors, ErrorCodes.Common.OPERATION_FAILED);
            }

            // ✅ حالا User در دیتابیس ذخیره شده و ID واقعی دارد
            // Domain Event را raise می‌کنیم و SaveChanges می‌زنیم تا interceptor آن را پردازش کند
            var correlationId = _correlationContext.GetCorrelationId();
            _logger.LogInformation("Raising UserRegisteredDomainEvent for user {UserId} with CorrelationId {CorrelationId}", user.Id, correlationId);
            _domainEvents.Raise(new UserRegisteredDomainEvent(
                UserId: user.Id,
                Email: user.Email!,
                PhoneNumber: user.PhoneNumber,
                FullName: null,
                OccurredOn: DateTimeOffset.UtcNow,
                CorrelationId: correlationId
            ));

            // این SaveChanges فقط برای Outbox است (User قبلاً ذخیره شده)
            // اما Interceptor domain events را می‌گیرد و در Outbox ذخیره می‌کند
            _logger.LogInformation("Calling SaveChangesAsync for outbox processing");
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("SaveChangesAsync completed");
            
            // ✅ Email/SMS sending moved to UserRegisteredNotificationHandler (async consumer)
            // This provides fast 201 response - confirmations happen in background
            var requireEmail = _emailSettings.RequireEmailConfirmation;
            var requirePhone = _phoneSettings.RequirePhoneConfirmation && !string.IsNullOrWhiteSpace(user.PhoneNumber);

            // 6) NextStep
            var next = RegisterNextStep.None;
            if (requireEmail && !user.EmailConfirmed) next = RegisterNextStep.ConfirmEmail;
            else if (requirePhone && !user.PhoneNumberConfirmed) next = RegisterNextStep.VerifyPhone;

            var response = new RegisterResponseDto(
                UserId: user.Id,
                RequireEmailConfirmation: requireEmail,
                EmailSent: requireEmail, // Will be sent async by consumer
                RequirePhoneConfirmation: requirePhone,
                PhoneCodeSent: requirePhone, // Will be sent async by consumer
                NextStep: next
            );

            _logger.LogInformation("User {UserId} registered (201). Email/SMS will be sent async. | DevId={DeviceId} UA={UA} IP={IP}",
                user.Id, deviceId, userAgent, ip);

            return Result<RegisterResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for email {Email}", MaskEmail(request.Email));
            return Result<RegisterResponseDto>.Failure(
                new[] { "general: An unexpected error occurred during registration." },
                ErrorCodes.Common.OPERATION_FAILED
            );
        }
    }

    #region Private Helpers

    private async Task<Result> CheckRateLimitsAsync(string email, string? ip, CancellationToken ct)
    {
        try
        {
            var normEmailLower = _userManager.NormalizeEmail(email)?.ToLowerInvariant() ?? email.ToLowerInvariant();

            var tasks = new List<Task<bool>>();

            tasks.Add(_rateLimiter.ShouldAllowAsync(
                $"reg:email:{normEmailLower}", 5, TimeSpan.FromMinutes(30), ct));

          
            if (!string.IsNullOrWhiteSpace(ip))
                tasks.Add(_rateLimiter.ShouldAllowAsync(
                    $"reg:ip:{ip}", 20, TimeSpan.FromMinutes(10), ct));

            if (!string.IsNullOrWhiteSpace(ip))
                tasks.Add(_rateLimiter.ShouldAllowAsync(
                    $"reg:emailip:{normEmailLower}:{ip}", 3, TimeSpan.FromMinutes(10), ct));

            var results = await Task.WhenAll(tasks);
            if (results.Any(allowed => allowed == false))
                return Result.Failure(
                    new[] { "[RATE_LIMIT] Too many registration attempts. Please try again later." },
                    ErrorCodes.Common.RATE_LIMIT_EXCEEDED);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RateLimit check failed");
            return Result.Success(); 
        }
    }

    private static RegisterRequestDto Normalize(RegisterRequestDto d) => d with
    {
        Email = d.Email.Trim().ToLowerInvariant(),
        PhoneNumber = string.IsNullOrWhiteSpace(d.PhoneNumber) ? null : d.PhoneNumber.Trim()
    };

    private static string MaskEmail(string e)
    {
        if (string.IsNullOrWhiteSpace(e) || !e.Contains('@')) return "***";
        var parts = e.Split('@'); var local = parts[0];
        var maskedLocal = local.Length <= 2 ? "*".PadLeft(local.Length, '*') : $"{local[0]}***{local[^1]}";
        return $"{maskedLocal}@{parts[1]}";
    }

    private static string MapField(IdentityError e)
        => e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "password"
            : e.Code.Contains("UserName", StringComparison.OrdinalIgnoreCase) ? "email"
            : "general";


    #endregion
}
