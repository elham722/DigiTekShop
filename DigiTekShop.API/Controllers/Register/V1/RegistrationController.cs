namespace DigiTekShop.API.Controllers.Register.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class RegistrationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(ISender sender, ILogger<RegistrationController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }



    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var result = await _sender.Send(new RegisterUserCommand(request), ct);

        return this.ToActionResult(result, createdLocationFactory: data =>
            Url.ActionLink(action: "", controller: "UsersQuery", values: new { version = "1.0", id = data.UserId })
        );
    }


}
