using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Controllers.Auth.V1
{
    [ApiController]
    [Route("account")]
    public class AccountPublicController : ControllerBase
    {
        private readonly IEmailConfirmationService _emailConfirm;

        public AccountPublicController(IEmailConfirmationService emailConfirm)
            => _emailConfirm = emailConfirm;

        // GET https://localhost:7055/account/confirm-email?userId=...&token=...
        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token, CancellationToken ct)
        {
            var res = await _emailConfirm.ConfirmEmailAsync(
                new ConfirmEmailRequestDto(userId, token), ct);

            if (res.IsFailure)
                return BadRequest(new { errors = res.Errors });

            // اگر فرانت داری، اینجا redirect کن به صفحه‌ی موفقیت
            // return Redirect("https://your-frontend/verify/success");
            return Ok(new { message = "Email confirmed." });
        }
    }

}
