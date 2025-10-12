using DigiTekShop.Contracts.Abstractions.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace DigiTekShop.API.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly ICacheService _cacheService;
    private const string IdempotencyKeyHeader = "X-Idempotency-Key";
    private const string IdempotencyCachePrefix = "idempotency:";

    public IdempotencyMiddleware(
        RequestDelegate next, 
        ILogger<IdempotencyMiddleware> logger,
        ICacheService cacheService)
    {
        _next = next;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        
        if (!IsIdempotentMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var idempotencyKey) ||
            StringValues.IsNullOrEmpty(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var key = $"{IdempotencyCachePrefix}{idempotencyKey}";
        
        try
        {
            var cachedResponse = await _cacheService.GetAsync<IdempotencyResponse>(key);
            
            if (cachedResponse != null)
            {
                _logger.LogInformation("Returning cached response for idempotency key: {Key}", idempotencyKey);
                
                context.Response.StatusCode = cachedResponse.StatusCode;
                context.Response.ContentType = cachedResponse.ContentType;
                
                if (!string.IsNullOrEmpty(cachedResponse.Headers))
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(cachedResponse.Headers);
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            context.Response.Headers[header.Key] = header.Value;
                        }
                    }
                }
                
                await context.Response.WriteAsync(cachedResponse.Body);
                return;
            }

            
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                
                var idempotencyResponse = new IdempotencyResponse
                {
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType ?? "application/json",
                    Body = responseBodyText,
                    Headers = JsonSerializer.Serialize(context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()))
                };

               
                await _cacheService.SetAsync(key, idempotencyResponse, TimeSpan.FromHours(24));
                
                _logger.LogInformation("Cached response for idempotency key: {Key}", idempotencyKey);
            }

            
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling idempotency for key: {Key}", idempotencyKey);
            await _next(context);
        }
    }

    private static bool IsIdempotentMethod(string method)
    {
        return method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("DELETE", StringComparison.OrdinalIgnoreCase);
    }
}

public class IdempotencyResponse
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Headers { get; set; } = string.Empty;
}

public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IdempotencyMiddleware>();
    }
}
