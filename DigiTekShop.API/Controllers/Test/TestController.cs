using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Controllers.Test;

/// <summary>
/// Controller برای تست‌های Integration و سلامت سیستم
/// (فقط در محیط Development/Test فعال می‌شود)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public sealed class TestController : ControllerBase
{
    /// <summary>
    /// یک endpoint ساده برای تست Rate Limiting
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong", timestamp = DateTimeOffset.UtcNow });
    }

    /// <summary>
    /// یک endpoint دیگر برای تست سطل‌های مستقل
    /// </summary>
    [HttpGet("other-ping")]
    public IActionResult OtherPing()
    {
        return Ok(new { message = "other-pong", timestamp = DateTimeOffset.UtcNow });
    }

    /// <summary>
    /// Endpoint برای تست‌های POST
    /// </summary>
    [HttpPost("echo")]
    public IActionResult Echo([FromBody] object payload)
    {
        return Ok(new { received = payload, timestamp = DateTimeOffset.UtcNow });
    }
}

