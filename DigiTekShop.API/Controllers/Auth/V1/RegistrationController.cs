using Asp.Versioning;
using DigiTekShop.API.Common;
using DigiTekShop.API.Controllers.Common.V1;
using DigiTekShop.API.Models;
using DigiTekShop.Application.Auth.Register.Command;
using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using MediatR;
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
    private readonly IMediator _mediator;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(IMediator mediator, ILogger<RegistrationController> logger)
    {
        _mediator = mediator;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Register

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var enriched = request with
        {
            DeviceId = request.DeviceId ?? ClientDeviceId,
            UserAgent = request.UserAgent ?? UserAgentHeader,
            IpAddress = request.IpAddress ?? ClientIp
        };

        var result = await _mediator.Send(new RegisterUserCommand(enriched), ct);
        return this.ToActionResult(result);
    }

    #endregion


    //#region Email_Confirm

    //[HttpPost("confirm-email")]
    //[AllowAnonymous]
    //[EnableRateLimiting("AuthPolicy")]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto request, CancellationToken ct)
    //{
    //    var result = await _mediator.Send(new RegisterUserCommand(request), ct);
    //    return this.ToActionResult(result);
    //}

    //#endregion


    //#region Email_Confirm_Resend

    //[HttpPost("resend-confirmation")]
    //[AllowAnonymous]
    //[EnableRateLimiting("AuthPolicy")]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailConfirmationRequestDto request, CancellationToken ct)
    //{
    //    var result = await _mediator.Send(new RegisterUserCommand(request), ct);
    //    return this.ToActionResult(result);
    //}

    //#endregion

}
