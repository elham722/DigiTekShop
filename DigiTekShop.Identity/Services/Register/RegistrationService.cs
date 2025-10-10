using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.Interfaces.Caching;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Options.PhoneVerification;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using DigiTekShop.Contracts.Enums.Auth;
using DigiTekShop.Identity.Options;

namespace DigiTekShop.Identity.Services.Register;

public sealed class RegistrationService : IRegistrationService
{
    private const string RATE_LIMIT_TAG = "[RATE_LIMIT]";

    private readonly UserManager<User> _userManager;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RegistrationService> _logger;
    private readonly PhoneVerificationSettings _phoneSettings;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly EmailConfirmationSettings _emailSettings;

    public RegistrationService(
        UserManager<User> userManager,
        IEmailConfirmationService emailConfirmationService,
        IPhoneVerificationService phoneVerificationService,
        IRateLimiter rateLimiter,
        IOptions<PhoneVerificationSettings> phoneOptions,
        IOptions<EmailConfirmationSettings> emailOptions, 
        DigiTekShopIdentityDbContext context,
        ILogger<RegistrationService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _emailConfirmationService = emailConfirmationService ?? throw new ArgumentNullException(nameof(emailConfirmationService));
        _phoneVerificationService = phoneVerificationService ?? throw new ArgumentNullException(nameof(phoneVerificationService));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _phoneSettings = phoneOptions?.Value ?? throw new ArgumentNullException(nameof(phoneOptions));
        _emailSettings = emailOptions?.Value ?? new EmailConfirmationSettings { RequireEmailConfirmation = true };
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        try
        {
            
            var rl = await CheckRateLimitsAsync(request.Email, request.IpAddress, ct);
            if (rl.IsFailure)
                return Result<RegisterResponseDto>.Failure(rl.Errors, rl.ErrorCode);

            var normalized = _userManager.NormalizeEmail(request.Email) ?? request.Email.ToUpperInvariant();

            var existsAny = await _context.Users
                .AsNoTracking()
                .IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalized, ct);


            if (existsAny)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                return Result<RegisterResponseDto>.Failure(
                    new[] { "email: Email already registered." },
                    ErrorCodes.Identity.UserExists
                );
            }

           
            var user = User.Create(request.Email,request.Email);

            if (request.PhoneNumber is not null)
                user.PhoneNumber = request.PhoneNumber;

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(e => $"password: {e.Description}").ToList();
                _logger.LogWarning("User creation failed for {Email}. Errors: {Errors}", request.Email, string.Join(" | ", errors));
                return Result<RegisterResponseDto>.Failure(errors, ErrorCodes.Common.OperationFailed);
            }

            var requireEmail = _emailSettings.RequireEmailConfirmation;
            var emailSent = false;
            string? emailError = null;
            if (requireEmail)
            {
                try
                {
                    var emailRes = await _emailConfirmationService.SendAsync(user.Id.ToString(), ct);
                    emailSent = emailRes.IsSuccess;
                    if (emailRes.IsFailure)
                    {
                        emailError = emailRes.GetFirstError(); // کمک بزرگ برای دیباگ
                        _logger.LogWarning("Email confirmation send failed for {Email}: {Err}",
                            request.Email, emailError);
                    }
                }
                catch (Exception ex)
                {
                    emailError = ex.Message;
                    _logger.LogError(ex, "Email confirmation exception for {Email}", request.Email);
                }
            }

            // 6) Phone verification (optional, best effort)
            var requirePhone = _phoneSettings.RequirePhoneConfirmation && !string.IsNullOrWhiteSpace(user.PhoneNumber);
            var phoneCodeSent = false;
            if (requirePhone)
            {
                try
                {
                    var phoneRes = await _phoneVerificationService.SendVerificationCodeAsync(user.Id, user.PhoneNumber!, ct);
                    phoneCodeSent = phoneRes.IsSuccess;
                    if (phoneRes.IsFailure)
                        _logger.LogWarning("Phone verification send failed for {Phone}: {Err}", user.PhoneNumber, phoneRes.GetFirstError());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Phone verification exception for {Phone}", user.PhoneNumber);
                }
            }

            // 7) Next step
            var next = RegisterNextStep.None;
            if (requireEmail && !user.EmailConfirmed) next = RegisterNextStep.ConfirmEmail;
            else if (requirePhone && !user.PhoneNumberConfirmed) next = RegisterNextStep.VerifyPhone;

            var response = new RegisterResponseDto(
                UserId: user.Id,
                RequireEmailConfirmation: requireEmail,
                EmailSent: emailSent,
                RequirePhoneConfirmation: requirePhone,
                PhoneCodeSent: phoneCodeSent,
                NextStep: next
            );

            _logger.LogInformation("User {UserId} registered. EmailSent={EmailSent}, PhoneCodeSent={PhoneCodeSent} | DevId={DeviceId} UA={UA} IP={IP}",
                user.Id, emailSent, phoneCodeSent, request.DeviceId, request.UserAgent, request.IpAddress);

            return Result<RegisterResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for email {Email}", request.Email);
            return Result<RegisterResponseDto>.Failure(
                new[] { "general: An unexpected error occurred during registration." },
                ErrorCodes.Common.OperationFailed
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
            if (!string.IsNullOrWhiteSpace(ip))
                tasks.Add(_rateLimiter.ShouldAllowAsync($"reg:ip:{ip}", 5, TimeSpan.FromMinutes(10), ct));
            tasks.Add(_rateLimiter.ShouldAllowAsync($"reg:email:{normEmailLower}", 3, TimeSpan.FromMinutes(10), ct));

            var results = await Task.WhenAll(tasks);
            if (results.Any(allowed => allowed == false))
                return Result.Failure(
                    new[] { "[RATE_LIMIT] Too many registration attempts. Please try again later." },
                    ErrorCodes.Common.RateLimitExceeded);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RateLimit check failed");
            return Result.Success(); // fail-open
        }
    }


    #endregion
}
