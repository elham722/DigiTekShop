using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Controllers.Common.V1
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected string? ClientDeviceId =>
            Request.Headers["X-Device-Id"].FirstOrDefault();

        protected string? UserAgentHeader =>
            Request.Headers["User-Agent"].FirstOrDefault();

        protected string? ClientIp =>
            Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? HttpContext.Connection.RemoteIpAddress?.ToString();
    }

}
