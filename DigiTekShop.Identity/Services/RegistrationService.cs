using DigiTekShop.Contracts.Interfaces.Caching;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using DigiTekShop.SharedKernel.Guards;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using DigiTekShop.Contracts.DTOs.Auth.PhoneVerification;
using DigiTekShop.Contracts.DTOs.Auth.Register;

namespace DigiTekShop.Identity.Services;

public class RegistrationService
{
    private const string RATE_LIMIT_TAG = "[RATE_LIMIT]";

    private readonly UserManager<User> _userManager;
    private readonly EmailConfirmationService _emailConfirmationService;
    private readonly PhoneVerificationService _phoneVerificationService;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RegistrationService> _logger;
    private readonly PhoneVerificationSettings _phoneSettings;
    private readonly DigiTekShopIdentityDbContext _context;

    public RegistrationService(
        UserManager<User> userManager,
        EmailConfirmationService emailConfirmationService,
        PhoneVerificationService phoneVerificationService,
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

    public async Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, string? ipAddress = null)
    {
        try
        {
            // 1) Validate (بدون Exception)
            var validationResult = ValidateRegistrationRequest(request);
            if (validationResult.IsFailure)
                return Result<RegisterResponseDto>.Failure(validationResult.Errors);

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var normalizedEmailUpper = normalizedEmail.ToUpperInvariant();

            // 2) Rate limit (با تگ ثابت)
            var rateLimitResult = await CheckRateLimitsAsync(normalizedEmail, ipAddress);
            if (rateLimitResult.IsFailure)
                return Result<RegisterResponseDto>.Failure(rateLimitResult.Errors);

            // 3) جلوگیری از ثبت‌نام حتی اگر کاربر Soft-Deleted باشد
            var existsAny = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalizedEmailUpper);

            if (existsAny)
            {
                _logger.LogWarning("Registration attempt with existing (any-state) email: {Email}", normalizedEmail);
                return Result<RegisterResponseDto>.Failure("Email already registered.");
            }

            // 4) Create user
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

            // 5) Email confirmation
            var emailSent = false;
            try
            {
                var emailResult = await _emailConfirmationService.SendConfirmationEmailAsync(user);
                emailSent = emailResult.IsSuccess;

                if (emailResult.IsFailure)
                    _logger.LogWarning("Failed to send email confirmation to {Email}: {Error}",
                        user.Email, emailResult.GetFirstError());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending email confirmation to {Email}", user.Email);
            }

            // 6) Phone verification (اختیاری)
            var phoneCodeSent = false;
            if (_phoneSettings.RequirePhoneConfirmation && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                try
                {
                    var phoneResult = await _phoneVerificationService.SendVerificationCodeAsync(user, user.PhoneNumber);
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

    private Result ValidateRegistrationRequest(RegisterRequestDto request)
    {
        var errors = new List<string>();

        // Email
        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email is required.");
        else
        {
            try { Guard.AgainstInvalidEmail(request.Email); }
            catch (DigiTekShop.SharedKernel.Exceptions.Validation.DomainValidationException)
            { errors.Add("Invalid email format."); }
        }

        // Passwords
        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Password is required.");

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            errors.Add("ConfirmPassword is required.");

        if (!string.IsNullOrWhiteSpace(request.Password) &&
            !string.IsNullOrWhiteSpace(request.ConfirmPassword) &&
            request.Password != request.ConfirmPassword)
            errors.Add("Passwords do not match.");

        // Terms
        if (!request.AcceptTerms)
            errors.Add("You must accept the terms and conditions.");

        // Phone
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            try { Guard.AgainstInvalidPhoneNumber(request.PhoneNumber); }
            catch (DigiTekShop.SharedKernel.Exceptions.Validation.DomainValidationException)
            { errors.Add("Invalid phone number format."); }

            if (!Regex.IsMatch(request.PhoneNumber, _phoneSettings.Security.AllowedPhonePattern))
                errors.Add("Invalid phone number format.");
        }

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

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
            // Fail-open در صورت خطای Redis/… (در Dev خوبه)
            return Result.Success();
        }
    }

    #endregion
}
