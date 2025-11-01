using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Controllers.Test;

[ApiController]
[ApiVersion("1.0")]
// فقط یک Route: هر دو فرم v1 و v1.0 را می‌پذیرد
[Route("api/v{version:regex(^1(\\.0)?$)}/test")]
[AllowAnonymous]
public sealed class TestController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
        => Ok(new { message = "pong", timestamp = DateTimeOffset.UtcNow });

    [HttpGet("other-ping")]
    public IActionResult OtherPing()
        => Ok(new { message = "other-pong", timestamp = DateTimeOffset.UtcNow });

    [HttpPost("echo")]
    public IActionResult Echo([FromBody] object payload)
        => Ok(new { received = payload, timestamp = DateTimeOffset.UtcNow });
}