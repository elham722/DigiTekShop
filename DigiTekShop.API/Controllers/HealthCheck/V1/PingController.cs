namespace DigiTekShop.API.Controllers.HealthCheck.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class PingController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get() => Ok(new { ok = true, time = DateTime.UtcNow });

    // تست Policy
    [Authorize(Policy = "Products.Manage")]
    [HttpGet("secure")]
    public IActionResult Secure() => Ok(new { ok = true, scope = "Products.Manage" });
}