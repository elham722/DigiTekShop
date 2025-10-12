using Asp.Versioning;
using DigiTekShop.API.Controllers.Common.V1;
using DigiTekShop.API.Extensions;
using DigiTekShop.API.Models;
using DigiTekShop.Application.Auth.Register.Command;
using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class RegistrationController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEmailConfirmationService _emailConfirm;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(IMediator mediator,
                                  IEmailConfirmationService emailConfirm,
                                  ILogger<RegistrationController> logger)
    {
        _mediator = mediator;
        _emailConfirm = emailConfirm;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var enriched = request with
        {
            DeviceId = request.DeviceId ?? ClientDeviceId,
            UserAgent = request.UserAgent ?? UserAgentHeader,
            IpAddress = request.IpAddress ?? ClientIp
        };

        var result = await _mediator.Send(new RegisterUserCommand(enriched), ct);
        //await _mediator.Send(new RegisterCustomerCommand(
        //    new RegisterCustomerDto(userId, model.FullName ?? model.Email, model.Email, model.PhoneNumber)
        //), ct);

        return this.ToActionResult(result);
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto dto, CancellationToken ct)
    {
        var result = await _emailConfirm.ConfirmEmailAsync(dto, ct);
        if (result.IsFailure) return this.ToActionResult(result); // تبدیل به 400 با ProblemDetails
        return NoContent();
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailConfirmationRequestDto dto, CancellationToken ct)
    {
        var result = await _emailConfirm.ResendAsync(dto, ct);
        if (result.IsFailure) return this.ToActionResult(result);
        return NoContent();
    }
}
