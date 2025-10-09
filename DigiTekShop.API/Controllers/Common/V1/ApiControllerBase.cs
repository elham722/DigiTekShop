using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DigiTekShop.API.Controllers.Common.V1
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected string? ClientDeviceId =>
            Request.Headers["X-Device-Id"].FirstOrDefault();

        protected string? UserAgentHeader =>
            Request.Headers["User-Agent"].FirstOrDefault();

       
        protected string? ClientIp
        {
            get
            {
                var xff = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(xff))
                {
                    var first = xff.Split(',')[0].Trim();
                    if (IPAddress.TryParse(first, out _))
                        return first;
                }

                var real = Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(real) && IPAddress.TryParse(real, out _))
                    return real;

                return HttpContext.Connection.RemoteIpAddress?.ToString();
            }
        }

        protected string? CorrelationId =>
            Request.Headers["X-Request-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
    }
}
