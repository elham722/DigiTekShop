using Asp.Versioning;
using DigiTekShop.API.Controllers.Common.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DigiTekShop.Contracts.DTOs.Auth.TwoFactor;
using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.API.ResultMapping;

namespace DigiTekShop.API.Controllers.TwoFactor.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class TwoFactorController : ApiControllerBase
{
    private readonly ITwoFactorService _twoFactor;
    private readonly ILogger<TwoFactorController> _logger;

    public TwoFactorController(ITwoFactorService twoFactor, ILogger<TwoFactorController> logger)
    {
        _twoFactor = twoFactor ?? throw new ArgumentNullException(nameof(twoFactor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("send")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> SendCode([FromBody] TwoFactorRequestDto request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _twoFactor.GenerateTwoFactorTokenAsync( request, ct);
        return this.ToActionResult(result);
    }

    [HttpPost("verify")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Verify([FromBody] VerifyTwoFactorRequestDto request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _twoFactor.VerifyTwoFactorTokenAsync(request, ct);
        return this.ToActionResult(result);
    }

}