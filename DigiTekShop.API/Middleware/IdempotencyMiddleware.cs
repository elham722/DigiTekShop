using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace DigiTekShop.API.Middleware;

public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly IdempotencyOptions _opts;

    private const string KeyHeader1 = "X-Idempotency-Key";
    private const string KeyHeader2 = "Idempotency-Key";
    private const string CachePrefix = "idempotency:";

    public IdempotencyMiddleware(
        RequestDelegate next,
        ILogger<IdempotencyMiddleware> logger,
        IOptions<IdempotencyOptions> options)
    {
        _next = next;
        _logger = logger;
        _opts = options.Value ?? new IdempotencyOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ct = context.RequestAborted;
        var cache = context.RequestServices.GetRequiredService<ICacheService>();
        var lockSvc = context.RequestServices.GetRequiredService<IDistributedLockService>();


        if (!IsWriteMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        
        if (!TryGetKey(context.Request.Headers, out var idemKey))
        {
            await _next(context);
            return;
        }

        
        var fingerprint = await BuildFingerprintAsync(context.Request, ct);

        var cacheKey = $"{CachePrefix}{idemKey}";
        var cached = await cache.GetAsync<IdempotencyResponse>(cacheKey, ct);
        if (cached is not null)
        {
            if (!string.Equals(cached.Fingerprint, fingerprint, StringComparison.Ordinal))
            {
                await WriteConflictAsync(context, idemKey, ct);
                return;
            }

            _logger.LogInformation("Idempotency hit: {Key}", idemKey);
            await WriteCachedAsync(context, cached, idemKey, ct);
            return;
        }

        
        var lockKey = $"{cacheKey}:lock";
        var gotLock = await lockSvc.AcquireAsync(lockKey, TimeSpan.FromSeconds(10), ct);
        if (!gotLock)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Duplicate request in progress. Please retry.", ct);
            return;
        }

        var originalBody = context.Response.Body;
        await using var mem = new MemoryStream();
        context.Response.Body = mem;

        try
        {
            await _next(context); 

            
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                
                if (mem.Length <= _opts.MaxBodySizeBytes)
                {
                    mem.Position = 0;
                    using var reader = new StreamReader(mem, leaveOpen: true);
                    var bodyText = await reader.ReadToEndAsync();

                    var headersDict = AllowHeaders(context.Response.Headers, _opts.AllowedHeaderNames);

                    var idemResp = new IdempotencyResponse
                    {
                        StatusCode = context.Response.StatusCode,
                        ContentType = context.Response.ContentType ?? "application/json",
                        Body = bodyText,
                        Headers = JsonSerializer.Serialize(headersDict),
                        Fingerprint = fingerprint
                    };

                    await cache.SetAsync(cacheKey, idemResp, TimeSpan.FromHours(_opts.TtlHours), ct);
                    _logger.LogInformation("Idempotency cached: {Key}", idemKey);
                }
            }

            
            context.Response.Headers["Idempotency-Key"] = idemKey;

            mem.Position = 0;
            await mem.CopyToAsync(originalBody, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Idempotency error (key={Key})", idemKey);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
            if (gotLock) 
                await lockSvc.ReleaseAsync(lockKey, ct);
        }
    }

    private static bool TryGetKey(IHeaderDictionary headers, out string key)
    {
        if (headers.TryGetValue(KeyHeader1, out var v1) && !StringValues.IsNullOrEmpty(v1))
        { key = v1.ToString(); return true; }
        if (headers.TryGetValue(KeyHeader2, out var v2) && !StringValues.IsNullOrEmpty(v2))
        { key = v2.ToString(); return true; }
        key = string.Empty; return false;
    }

    private static bool IsWriteMethod(string m)
        => m.Equals("POST", StringComparison.OrdinalIgnoreCase)
        || m.Equals("PUT", StringComparison.OrdinalIgnoreCase)
        || m.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
        || m.Equals("DELETE", StringComparison.OrdinalIgnoreCase);

    private static async Task<string> BuildFingerprintAsync(HttpRequest req, CancellationToken ct)
    {
        req.EnableBuffering();

        using var sha = System.Security.Cryptography.SHA256.Create();

        
        await using var ms = new MemoryStream();
        await req.Body.CopyToAsync(ms, ct);
        var bodyBytes = ms.ToArray();
        req.Body.Position = 0;
        var bodyHash = Convert.ToBase64String(sha.ComputeHash(bodyBytes));

        var userId = req.HttpContext.User?.Identity?.IsAuthenticated == true
            ? (req.HttpContext.User.FindFirst("sub")?.Value ?? req.HttpContext.User.Identity?.Name)
            : null;

        var canonicalQuery = CanonicalQuery(req.Query);
        var input = $"{req.Method}|{req.Path}?{canonicalQuery}|{userId}|{bodyHash}";
        var fp = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
        return fp;
    }

    private static async Task WriteConflictAsync(HttpContext ctx, string key, CancellationToken ct)
    {
        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        ctx.Response.ContentType = "application/problem+json";

        var pd = new ProblemDetails
        {
            Type = "urn:problem:IDEMPOTENCY_KEY_REUSE_DIFFERENT_BODY",
            Title = "Idempotency conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = "The same idempotency key was used with a different request payload.",
            Instance = ctx.Request.Path
        };
        pd.Extensions["key"] = key;

        await ctx.Response.WriteAsJsonAsync(pd, ct);
    }

    private static async Task WriteCachedAsync(HttpContext ctx, IdempotencyResponse cached, string idemKey, CancellationToken ct)
    {
        ctx.Response.StatusCode = cached.StatusCode;
        ctx.Response.ContentType = cached.ContentType;

        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(cached.Headers) ?? new();

        foreach (var kv in headers)
            ctx.Response.Headers[kv.Key] = kv.Value;

        
        ctx.Response.Headers["Idempotency-Key"] = idemKey;
        ctx.Response.Headers["Idempotent-Replay"] = "true";

        await ctx.Response.WriteAsync(cached.Body, ct);
    }

    private static Dictionary<string, string> AllowHeaders(IHeaderDictionary headers, string[] allowList)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in allowList)
            if (headers.TryGetValue(h, out var v)) dict[h] = v.ToString();
        return dict;
    }

    private static string CanonicalQuery(IQueryCollection q) =>
        string.Join("&", q.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .SelectMany(kv => kv.Value.Order().Select(v => $"{kv.Key}={v}")));

}

