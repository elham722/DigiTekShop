using Asp.Versioning;
using DigiTekShop.API.Common;
using DigiTekShop.API.Controllers.Common.V1;
using DigiTekShop.API.Models;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.DTOs.Auth.Token;
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
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Registration & Email Confirmation

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var enriched = request with { DeviceId = request.DeviceId ?? ClientDeviceId, UserAgent = request.UserAgent ?? UserAgentHeader, Ip = request.Ip ?? ClientIp };
        var result = await _authService.RegisterAsync(enriched, cancellationToken);
        return this.ToActionResult(result);
    }

    #endregion

    #region Authentication

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var enriched = request with { DeviceId = request.DeviceId ?? ClientDeviceId, UserAgent = request.UserAgent ?? UserAgentHeader, Ip = request.Ip ?? ClientIp };
        var result = await _authService.LoginAsync(enriched, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request, CancellationToken cancellationToken = default)
    {

        var enriched = request with { DeviceId = request.DeviceId ?? ClientDeviceId, UserAgent = request.UserAgent ?? UserAgentHeader, Ip = request.Ip ?? ClientIp };
        var result = await _authService.RefreshAsync(enriched, cancellationToken);
        return this.ToActionResult(result);
    }
    #endregion

 

}
