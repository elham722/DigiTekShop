namespace DigiTekShop.API.Controllers.Auth.V1;

[ApiController]
[Route("account")]
public class AccountPublicController : ControllerBase
{
    private readonly IEmailConfirmationService _emailConfirm;

    public AccountPublicController(IEmailConfirmationService emailConfirm)
        => _emailConfirm = emailConfirm;

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token, CancellationToken ct)
    {
        var res = await _emailConfirm.ConfirmEmailAsync(
            new ConfirmEmailRequestDto(userId, token), ct);

        return this.ToActionResult(res);
    }
}
