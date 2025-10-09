using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.Interfaces.Caching;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Options.PhoneVerification;
using DigiTekShop.SharedKernel.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigiTekShop.Identity.Services.Register;

public class RegistrationService : IRegistrationService
{
    private const string RATE_LIMIT_TAG = "[RATE_LIMIT]";

    private readonly UserManager<User> _userManager;
    private readonly IEmailConfirmationService _emailConfirmationService;   
    private readonly IPhoneVerificationService _phoneVerificationService;   
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RegistrationService> _logger;
    private readonly PhoneVerificationSettings _phoneSettings;
    private readonly DigiTekShopIdentityDbContext _context;

    public RegistrationService(
        UserManager<User> userManager,
        IEmailConfirmationService emailConfirmationService,
        IPhoneVerificationService phoneVerificationService,
        IRateLimiter rateLimiter,
        IOptions<PhoneVerificationSettings> phoneOptions,
        DigiTekShopIdentityDbContext context,
        ILogger<RegistrationService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _emailConfirmationService = emailConfirmationService ?? throw new ArgumentNullException(nameof(emailConfirmationService));
        _phoneVerificationService = phoneVerificationService ?? throw new ArgumentNullException(nameof(phoneVerificationService));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _phoneSettings = phoneOptions?.Value ?? throw new ArgumentNullException(nameof(phoneOptions));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

   
    public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        try
        {

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var normalizedEmailUpper = normalizedEmail.ToUpperInvariant();

           
            var rateLimitResult = await CheckRateLimitsAsync(normalizedEmail, ipAddress: request.IpAddress);
            if (rateLimitResult.IsFailure)
                return Result<RegisterResponseDto>.Failure(rateLimitResult.Errors);

            
            var existsAny = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalizedEmailUpper, ct);

            if (existsAny)
            {
                _logger.LogWarning("Registration attempt with existing (any-state) email: {Email}", normalizedEmail);
                return Result<RegisterResponseDto>.Failure("Email already registered.");
            }

            
            var user = User.Create(normalizedEmail, normalizedEmail);

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber.Trim();

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(e => e.Description);
                _logger.LogWarning("User creation failed for email {Email}. Errors: {Errors}",
                    normalizedEmail, string.Join(", ", errors));
                return Result<RegisterResponseDto>.Failure($"Registration failed: {string.Join(", ", errors)}");
            }

           
            var emailSent = false;
            try
            {
                var emailResult = await _emailConfirmationService.SendAsync(user.Id.ToString(), ct);
                emailSent = emailResult.IsSuccess;

                if (emailResult.IsFailure)
                    _logger.LogWarning("Failed to send email confirmation to {Email}: {Error}",
                        user.Email, emailResult.GetFirstError());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending email confirmation to {Email}", user.Email);
            }

           
            var phoneCodeSent = false;
            if (_phoneSettings.RequirePhoneConfirmation && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                try
                {
                    var phoneResult = await _phoneVerificationService.SendVerificationCodeAsync(user.Id, user.PhoneNumber);
                    phoneCodeSent = phoneResult.IsSuccess;

                    if (phoneResult.IsFailure)
                        _logger.LogWarning("Failed to send phone verification code to {Phone}: {Error}",
                            user.PhoneNumber, phoneResult.GetFirstError());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception while sending phone verification code to {Phone}", user.PhoneNumber);
                }
            }

            var response = new RegisterResponseDto(
                UserId: user.Id,
                RequireEmailConfirmation: true,
                EmailSent: emailSent,
                RequirePhoneConfirmation: _phoneSettings.RequirePhoneConfirmation,
                PhoneCodeSent: phoneCodeSent
            );

            _logger.LogInformation("User {UserId} registered. EmailSent={EmailSent}, PhoneCodeSent={PhoneCodeSent}",
                user.Id, emailSent, phoneCodeSent);

            return Result<RegisterResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for email {Email}", request.Email);
            return Result<RegisterResponseDto>.Failure("An unexpected error occurred during registration.");
        }
    }


    #region Private Helpers

    private async Task<Result> CheckRateLimitsAsync(string normalizedEmail, string? ipAddress)
    {
        try
        {
            var ipKey = $"reg:ip:{ipAddress ?? "unknown"}";
            var ipAllowed = await _rateLimiter.ShouldAllowAsync(ipKey, 5, TimeSpan.FromMinutes(10));
            if (!ipAllowed)
            {
                _logger.LogWarning("Registration rate limit exceeded for IP: {Ip}", ipAddress);
                return Result.Failure($"{RATE_LIMIT_TAG} Too many registration attempts from this IP. Please try again later.");
            }

            var emailKey = $"reg:email:{normalizedEmail}";
            var emailAllowed = await _rateLimiter.ShouldAllowAsync(emailKey, 3, TimeSpan.FromMinutes(10));
            if (!emailAllowed)
            {
                _logger.LogWarning("Registration rate limit exceeded for email: {Email}", normalizedEmail);
                return Result.Failure($"{RATE_LIMIT_TAG} Too many registration attempts for this email. Please try again later.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limits for email {Email}, IP {Ip}", normalizedEmail, ipAddress);
            
            return Result.Success();
        }
    }

    #endregion
}
