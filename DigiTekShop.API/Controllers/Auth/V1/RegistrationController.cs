using Asp.Versioning;
using DigiTekShop.API.Common;
using DigiTekShop.API.Controllers.Common.V1;
using DigiTekShop.API.Models;
using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigiTekShop.API.Controllers.Auth.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class RegistrationController : ApiControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(IRegistrationService registrationService, IEmailConfirmationService emailConfirmationService, ILogger<RegistrationController> logger)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _emailConfirmationService= emailConfirmationService ?? throw new ArgumentNullException(nameof(emailConfirmationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Register

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var enriched = request with
        {
            DeviceId = request.DeviceId ?? ClientDeviceId,
            UserAgent = request.UserAgent ?? UserAgentHeader,
            Ip = request.Ip ?? ClientIp
        };

        var result = await _registrationService.RegisterAsync(enriched, ct);
        return this.ToActionResult(result);
    }

    #endregion


    #region Email_Confirm

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto request, CancellationToken ct)
    {
        var result = await _emailConfirmationService.ConfirmEmailAsync(request, ct);
        return this.ToActionResult(result);
    }

    #endregion


    #region Email_Confirm_Resend

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailConfirmationRequestDto request, CancellationToken ct)
    {
        var result = await _emailConfirmationService.ResendAsync(request, ct);
        return this.ToActionResult(result);
    }

    #endregion

}
