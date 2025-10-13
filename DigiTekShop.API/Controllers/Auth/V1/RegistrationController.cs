namespace DigiTekShop.API.Controllers.Auth.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class RegistrationController : ControllerBase
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
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterUserCommand(request), ct);

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
        if (result.IsFailure) return this.ToActionResult(result);
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
