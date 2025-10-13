namespace DigiTekShop.API.Controllers.Auth.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ILoginService _loginService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILoginService loginService, ILogger<AuthController> logger)
    {
        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    #region Login

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _loginService.LoginAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }


    #endregion

    #region Refresh Token

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _loginService.RefreshAsync(request, cancellationToken);
        return this.ToActionResult(result);
    }

    #endregion

    #region Logout

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request, CancellationToken ct)
    {
       
        var result = await _loginService.LogoutAsync(request, ct);
        return this.ToActionResult(result);
    }

    #endregion

    #region Logout-all

    [HttpPost("logout-all")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Problem("UserId not found in token", statusCode: StatusCodes.Status401Unauthorized, title: "UNAUTHORIZED");

        var result = await _loginService.LogoutAllDevicesAsync(userId, ct);
        return this.ToActionResult(result);
    }

    #endregion
}
