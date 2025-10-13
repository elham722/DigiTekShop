namespace DigiTekShop.API.Controllers.HealthCheck.V1;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        });
    }

    /// <summary>
    /// Redis health check endpoint
    /// </summary>
    [HttpGet("redis")]
    public IActionResult GetRedisHealth()
    {
        // This will be handled by the built-in health checks
        return Ok(new
        {
            Message = "Use /health endpoint for detailed health checks",
            RedisEndpoint = "/health"
        });
    }
}
