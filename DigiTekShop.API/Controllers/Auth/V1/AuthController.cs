using DigiTekShop.Application.Auth.Login.Command;
using DigiTekShop.Application.Auth.Logout.Command;
using DigiTekShop.Application.Auth.LogoutAll.Command;
using DigiTekShop.Application.Auth.Me.Query;
using DigiTekShop.Application.Auth.Mfa.Command;
using DigiTekShop.Application.Auth.Tokens.Command;
using DigiTekShop.Contracts.DTOs.Auth.Me;
using DigiTekShop.Contracts.DTOs.Auth.Mfa;

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

    #region LOGIN

 
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<LoginResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Login attempt for: {Login}", request.Login);
        
        var result = await _sender.Send(new LoginCommand(request), ct);
        
        if (result.IsSuccess && result.Value?.IsSuccess == true)
        {
            _logger.LogInformation("Login successful for: {Login}", request.Login);
        }
        else if (result.IsSuccess && result.Value?.IsChallenge == true)
        {
            _logger.LogInformation("MFA challenge issued for: {Login}", request.Login);
        }
        
        return this.ToActionResult(result);
    }

    #endregion

    #region VERIFY MFA

    [HttpPost("verify-mfa")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request, CancellationToken ct)
    {
        _logger.LogInformation("MFA verification attempt for user: {UserId}", request.UserId);
        
        var result = await _sender.Send(new VerifyMfaCommand(request), ct);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("MFA verification successful for user: {UserId}", request.UserId);
        }
        
        return this.ToActionResult(result);
    }

    #endregion

    #region REFRESH TOKEN

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Token refresh attempt");
        
        var result = await _sender.Send(new RefreshTokenCommand(request), ct);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Token refresh successful");
        }
        
        return this.ToActionResult(result);
    }

    #endregion

    #region LOGOUT

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Logout attempt for user: {UserId}", request.UserId);
        
        var result = await _sender.Send(new LogoutCommand(request), ct);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Logout successful for user: {UserId}", request.UserId);
        }
        
        return this.ToActionResult(result);
    }

    #endregion

    #region LOGOUT ALL

    [HttpPost("logout-all")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll([FromBody] LogoutAllRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Logout all sessions attempt for user: {UserId}", request.UserId);
        
        var result = await _sender.Send(new LogoutAllCommand(request), ct);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("Logout all sessions successful for user: {UserId}", request.UserId);
        }
        
        return this.ToActionResult(result);
    }

    #endregion

    #region ME

    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("ApiPolicy")]
    [ProducesResponseType(typeof(ApiResponse<MeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.Identity?.Name;
        _logger.LogInformation("Get current user info for: {UserId}", userId);
        
        var result = await _sender.Send(new MeQuery(), ct);
        
        return this.ToActionResult(result);
    }

    #endregion
}
