using DigiTekShop.Identity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DigiTekShop.Identity.Options;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Identity.Options.PhoneVerification; // PasswordPolicyOptions

namespace DigiTekShop.API.Controllers;

/// <summary>
/// Authentication controller handling user registration and authentication
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegistrationService _registrationService;
    private readonly ILogger<AuthController> _logger;
    private readonly PhoneVerificationSettings _phoneSettings;
    private readonly PasswordPolicyOptions _pwd;

    public AuthController(
        RegistrationService registrationService,
        IOptions<PhoneVerificationSettings> phoneOptions,
        IOptions<PasswordPolicyOptions> passwordPolicy,
        ILogger<AuthController> logger)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _phoneSettings = phoneOptions.Value ?? throw new ArgumentNullException(nameof(phoneOptions));
        _pwd = passwordPolicy.Value ?? throw new ArgumentNullException(nameof(passwordPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Registration result</returns>
    /// <response code="200">Registration successful</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            // Get client IP address for rate limiting
            var ipAddress = GetClientIpAddress();
            
            _logger.LogInformation("Registration attempt from IP {IpAddress} for email {Email}", 
                ipAddress, request.Email);

            // Call registration service
            var result = await _registrationService.RegisterAsync(request, ipAddress);

            if (result.IsFailure)
            {
                var first = result.GetFirstError();

                if (first?.StartsWith("[RATE_LIMIT]") == true)
                {
                    return StatusCode(StatusCodes.Status429TooManyRequests, new
                    {
                        message = first,
                        errorCode = "RATE_LIMIT_EXCEEDED"
                    });
                }

                return BadRequest(new
                {
                    message = first,
                    errorCode = "VALIDATION_FAILED"
                });
            }

            _logger.LogInformation("Registration successful for user {UserId}", result.Value.UserId);
            
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for email {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred during registration.",
                errorCode = "INTERNAL_SERVER_ERROR"
            });
        }
    }

    [HttpGet("registration-info")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetRegistrationInfo()
    {
        return Ok(new
        {
            requirements = new
            {
                emailConfirmationRequired = true,
                phoneConfirmationRequired = _phoneSettings.RequirePhoneConfirmation,
                passwordPolicy = new
                {
                    minLength = _pwd.MinLength,
                    requiredCategoryCount = _pwd.RequiredCategoryCount,
                    maxRepeatedChar = _pwd.MaxRepeatedChar,
                    historyDepth = _pwd.HistoryDepth,
                    forbidUserNameFragments = _pwd.ForbidUserNameFragments,
                    forbidEmailLocalPart = _pwd.ForbidEmailLocalPart
                },
                phonePattern = _phoneSettings.Security.AllowedPhonePattern,
                rateLimits = new
                {
                    ipLimit = "5 per 10 minutes",
                    emailLimit = "3 per 10 minutes"
                }
            },
            features = new[]
            {
                "Email confirmation",
                "Phone verification",
                "Rate limiting",
                "Password policy enforcement",
                "Audit logging"
            }
        });
    }


    #region Private Helpers

    private string GetClientIpAddress()
    {
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Check for real IP header
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fall back to connection remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    #endregion
}

