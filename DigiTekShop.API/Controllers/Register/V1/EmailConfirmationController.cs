using DigiTekShop.Application.Auth.ConfirmEmail.Command;
using DigiTekShop.Application.Auth.ResendEmailConfirmation.Command;
using MediatR;

namespace DigiTekShop.API.Controllers.Register.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/account")]
public sealed class EmailConfirmationController : ControllerBase
{
    private readonly ISender _sender;

    public EmailConfirmationController(ISender sender)
        => _sender = sender ?? throw new ArgumentNullException(nameof(sender));

   
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting("EmailConfirmPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token, CancellationToken ct)
    {
        var result = await _sender.Send(new ConfirmEmailCommand(new ConfirmEmailRequestDto(userId, token)), ct);
        return this.ToActionResult(result);
    }

  
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting("EmailConfirmPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new ConfirmEmailCommand(dto), ct);
        return this.ToActionResult(result);
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting("EmailConfirmPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailConfirmationRequestDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new ResendEmailConfirmationCommand(dto), ct);
        return this.ToActionResult(result);
    }
}
