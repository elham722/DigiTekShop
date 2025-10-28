using DigiTekShop.Application.Auth.LoginOrRegister.Commands;
using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;

namespace DigiTekShop.API.Controllers.Auth.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Consumes("application/json")]
[Tags("Authentication")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISender sender, ILogger<AuthController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region LoginOrRegister

    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new SendOtpCommand(dto), ct);
        return this.ToActionResult(result);
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new VerifyOtpCommand(dto), ct);
        return this.ToActionResult(result);
    }


    #endregion

    #region REFRESH TOKEN

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["cid"] = HttpContext.TraceIdentifier
        });

        _logger.LogInformation("Token refresh attempt");

        var result = await _sender.Send(new RefreshTokenCommand(request), ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Token refresh success");
        }

        return this.ToActionResult(result);
    }

    #endregion

    #region LOGOUT

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["cid"] = HttpContext.TraceIdentifier,
            ["userId"] = request.UserId
        });
        _logger.LogInformation("Logout attempt");
        var result = await _sender.Send(new LogoutCommand(request), ct);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Logout success");
        }
        
        return this.ToActionResult(result);
    }

    #endregion

    #region LOGOUT ALL

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll([FromBody] LogoutAllRequest request, CancellationToken ct)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["cid"] = HttpContext.TraceIdentifier,
            ["userId"] = request.UserId
        });

        _logger.LogInformation("Logout all sessions attempt");

        var result = await _sender.Send(new LogoutAllCommand(request), ct);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Logout all sessions success");
        }
        
        return this.ToActionResult(result);
    }

    #endregion

    #region ME

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<MeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _sender.Send(new MeQuery(), ct);
        return this.ToActionResult(result);
    }

    #endregion
}
