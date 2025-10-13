using DigiTekShop.API.Common.Api;

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
    [ProducesResponseType(typeof(ApiResponse<CacheSetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetCache([FromBody] CacheSetRequest request)
    {
        try
        {
            await _cacheService.SetAsync(request.Key, request.Value, TimeSpan.FromMinutes(request.TtlMinutes ?? 60));
            
            _logger.LogInformation("Cache set for key: {Key}", request.Key);
            var response = new CacheSetResponse("Value cached successfully", request.Key);
            return Ok(new ApiResponse<CacheSetResponse>(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", request.Key);
            return StatusCode(500, new ProblemDetails
            {
                Type = "urn:problem:CACHE_ERROR",
                Title = "Cache Error",
                Status = 500,
                Detail = "Error setting cache",
                Instance = Request.Path
            });
        }
    }

    /// <summary>
    /// Gets a value from cache
    /// </summary>
    [HttpGet("get/{key}")]
    [ProducesResponseType(typeof(ApiResponse<CacheGetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCache(string key)
    {
        try
        {
            var value = await _cacheService.GetAsync<object>(key);
            
            if (value == null)
            {
                return NotFound(new ProblemDetails
                {
                    Type = "urn:problem:CACHE_KEY_NOT_FOUND",
                    Title = "Cache Key Not Found",
                    Status = 404,
                    Detail = "Key not found in cache",
                    Instance = Request.Path
                });
            }

            var response = new CacheGetResponse(key, value);
            return Ok(new ApiResponse<CacheGetResponse>(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            return StatusCode(500, new ProblemDetails
            {
                Type = "urn:problem:CACHE_ERROR",
                Title = "Cache Error",
                Status = 500,
                Detail = "Error getting cache",
                Instance = Request.Path
            });
        }
    }

    /// <summary>
    /// Removes a value from cache
    /// </summary>
    [HttpDelete("{key}")]
    [ProducesResponseType(typeof(ApiResponse<CacheRemoveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveCache(string key)
    {
        try
        {
            await _cacheService.RemoveAsync(key);
            
            _logger.LogInformation("Cache removed for key: {Key}", key);
            var response = new CacheRemoveResponse("Value removed from cache", key);
            return Ok(new ApiResponse<CacheRemoveResponse>(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            return StatusCode(500, new ProblemDetails
            {
                Type = "urn:problem:CACHE_ERROR",
                Title = "Cache Error",
                Status = 500,
                Detail = "Error removing cache",
                Instance = Request.Path
            });
        }
    }

    /// <summary>
    /// Tests rate limiting
    /// </summary>
    [HttpPost("rate-limit-test")]
    [ProducesResponseType(typeof(ApiResponse<RateLimitTestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
                return StatusCode(429, new ProblemDetails
                {
                    Type = "urn:problem:RATE_LIMIT_EXCEEDED",
                    Title = "Rate Limit Exceeded",
                    Status = 429,
                    Detail = "Rate limit exceeded",
                    Instance = Request.Path
                });
            }

            var response = new RateLimitTestResponse("Request allowed", request.Key, request.Limit, request.WindowMinutes);
            return Ok(new ApiResponse<RateLimitTestResponse>(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing rate limit for key: {Key}", request.Key);
            return StatusCode(500, new ProblemDetails
            {
                Type = "urn:problem:RATE_LIMIT_ERROR",
                Title = "Rate Limit Error",
                Status = 500,
                Detail = "Error testing rate limit",
                Instance = Request.Path
            });
        }
    }

    /// <summary>
    /// Gets cache statistics (demo)
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<CacheStatsResponse>), StatusCodes.Status200OK)]
    public IActionResult GetCacheStats()
    {
        var response = new CacheStatsResponse(
            "DigiTekShop Cache Service",
            DateTime.UtcNow,
            new[] { "Redis-based distributed caching", "Rate limiting with fixed window", "Data protection key ring", "Health monitoring" },
            new CacheEndpointsInfo("/health", "/health", "POST /api/cache/set", "GET /api/cache/get/{key}", "DELETE /api/cache/{key}", "POST /api/cache/rate-limit-test")
        );
        return Ok(new ApiResponse<CacheStatsResponse>(response));
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

/// <summary>
/// Response model for cache set operation
/// </summary>
public record CacheSetResponse(string Message, string Key);

/// <summary>
/// Response model for cache get operation
/// </summary>
public record CacheGetResponse(string Key, object Value);

/// <summary>
/// Response model for cache remove operation
/// </summary>
public record CacheRemoveResponse(string Message, string Key);

/// <summary>
/// Response model for rate limit test
/// </summary>
public record RateLimitTestResponse(string Message, string? Key, int? Limit, int? WindowMinutes);

/// <summary>
/// Response model for cache statistics
/// </summary>
public record CacheStatsResponse(string Service, DateTime Timestamp, string[] Features, CacheEndpointsInfo Endpoints);

/// <summary>
/// Endpoints information for cache stats
/// </summary>
public record CacheEndpointsInfo(string Health, string RedisHealth, string CacheSet, string CacheGet, string CacheRemove, string RateLimitTest);
