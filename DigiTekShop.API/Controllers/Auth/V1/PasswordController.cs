namespace DigiTekShop.API.Controllers.Auth.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class PasswordController : ControllerBase
{
    private readonly IPasswordService _passwordService;
    private readonly ILogger<PasswordController> _logger;

    public PasswordController(IPasswordService passwordService, ILogger<PasswordController> logger)
    {
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region FORGOT PASSWORD 

    [HttpPost("forgot")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequestDto request, CancellationToken ct)
    {
        var result = await _passwordService.ForgotPasswordAsync(request, ct);
        return this.ToActionResult(result);
    }

    #endregion


    #region RESET PASSWORD 
    [HttpPost("reset")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequestDto request, CancellationToken ct)
    {
        var result = await _passwordService.ResetPasswordAsync(request, ct);
        return this.ToActionResult(result);
    }

    #endregion

    #region Change PASSWORD 
    [HttpPost("change")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Change([FromBody] ChangePasswordRequestDto request, CancellationToken ct)
    {
        var result = await _passwordService.ChangePasswordAsync(request, ct);
        return this.ToActionResult(result);
    }

    #endregion

}
