namespace DigiTekShop.API.Controllers.Cache.V1;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheService cacheService,
        IRateLimiter rateLimiter,
        ILogger<CacheController> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sets a value in cache
    /// </summary>
    [HttpPost("set")]
    public async Task<IActionResult> SetCache([FromBody] CacheSetRequest request)
    {
        try
        {
            await _cacheService.SetAsync(request.Key, request.Value, TimeSpan.FromMinutes(request.TtlMinutes ?? 60));
            
            _logger.LogInformation("Cache set for key: {Key}", request.Key);
            return Ok(new { Message = "Value cached successfully", request.Key });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", request.Key);
            return StatusCode(500, new { Message = "Error setting cache", Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a value from cache
    /// </summary>
    [HttpGet("get/{key}")]
    public async Task<IActionResult> GetCache(string key)
    {
        try
        {
            var value = await _cacheService.GetAsync<object>(key);
            
            if (value == null)
            {
                return NotFound(new { Message = "Key not found in cache", Key = key });
            }

            return Ok(new { Key = key, Value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            return StatusCode(500, new { Message = "Error getting cache", Error = ex.Message });
        }
    }

    /// <summary>
    /// Removes a value from cache
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> RemoveCache(string key)
    {
        try
        {
            await _cacheService.RemoveAsync(key);
            
            _logger.LogInformation("Cache removed for key: {Key}", key);
            return Ok(new { Message = "Value removed from cache", Key = key });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            return StatusCode(500, new { Message = "Error removing cache", Error = ex.Message });
        }
    }

    /// <summary>
    /// Tests rate limiting
    /// </summary>
    [HttpPost("rate-limit-test")]
    public async Task<IActionResult> TestRateLimit([FromBody] RateLimitTestRequest request)
    {
        try
        {
            var isAllowed = await _rateLimiter.ShouldAllowAsync(
                request.Key ?? "default_test",
                request.Limit ?? 5,
                TimeSpan.FromMinutes(request.WindowMinutes ?? 1));

            if (!isAllowed)
            {
                return StatusCode(429, new 
                { 
                    Message = "Rate limit exceeded",
                    request.Key,
                    request.Limit,
                    request.WindowMinutes
                });
            }

            return Ok(new 
            { 
                Message = "Request allowed",
                request.Key,
                request.Limit,
                request.WindowMinutes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing rate limit for key: {Key}", request.Key);
            return StatusCode(500, new { Message = "Error testing rate limit", Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets cache statistics (demo)
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetCacheStats()
    {
        return Ok(new
        {
            Service = "DigiTekShop Cache Service",
            Timestamp = DateTime.UtcNow,
            Features = new[]
            {
                "Redis-based distributed caching",
                "Rate limiting with fixed window",
                "Data protection key ring",
                "Health monitoring"
            },
            Endpoints = new
            {
                Health = "/health",
                RedisHealth = "/health",
                CacheSet = "POST /api/cache/set",
                CacheGet = "GET /api/cache/get/{key}",
                CacheRemove = "DELETE /api/cache/{key}",
                RateLimitTest = "POST /api/cache/rate-limit-test"
            }
        });
    }
}

/// <summary>
/// Request model for setting cache
/// </summary>
public record CacheSetRequest(string Key, object Value, int? TtlMinutes = null);

/// <summary>
/// Request model for testing rate limit
/// </summary>
public record RateLimitTestRequest(string? Key = null, int? Limit = null, int? WindowMinutes = null);
