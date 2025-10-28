using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.Contracts.Options.Phone;
using DigiTekShop.Contracts.Abstractions.Caching;
using Microsoft.Extensions.Options;

namespace DigiTekShop.API.Middleware;

public sealed class RedisRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RedisRateLimitMiddleware> _logger;
    private readonly IRateLimiter _rateLimiter;
    private readonly LoginFlowOptions _loginFlow;
    private readonly PhoneVerificationOptions _phoneOpts;

    public RedisRateLimitMiddleware(
        RequestDelegate next,
        ILogger<RedisRateLimitMiddleware> logger,
        IRateLimiter rateLimiter,
        IOptions<LoginFlowOptions> loginFlow,
        IOptions<PhoneVerificationOptions> phoneOpts)
    {
        _next = next;
        _logger = logger;
        _rateLimiter = rateLimiter;
        _loginFlow = loginFlow.Value;
        _phoneOpts = phoneOpts.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // --- bypass ---
        var m = context.Request.Method;
        if (HttpMethods.IsOptions(m) || HttpMethods.IsHead(m))
        { await _next(context); return; }

        var p = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (p.StartsWith("/swagger") || p.StartsWith("/health") || p.StartsWith("/favicon"))
        { await _next(context); return; }

        var cfg = await GetRateLimitConfigAsync(context);
        if (cfg is null)
        { await _next(context); return; }

        var key = cfg.Key;
        var decision = await _rateLimiter.ShouldAllowAsync(key, cfg.Limit, cfg.Window, context.RequestAborted);

        // headers
        context.Response.Headers["X-RateLimit-Policy"] = cfg.Policy;
        context.Response.Headers["X-RateLimit-Limit"] = cfg.Limit.ToString();
        context.Response.Headers["X-RateLimit-Window"] = ((int)cfg.Window.TotalSeconds).ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, cfg.Limit - (int)decision.Count).ToString();
        if (decision.Ttl is { } ttl)
            context.Response.Headers["Retry-After"] = Math.Max(0, (int)ttl.TotalSeconds).ToString();

        if (!decision.Allowed)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            _logger.LogWarning("429 RateLimit policy={Policy} key={Key} path={Path}", cfg.Policy, key, context.Request.Path);
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "RATE_LIMIT_EXCEEDED",
                policy = cfg.Policy,
                limit = cfg.Limit,
                window = (int)cfg.Window.TotalSeconds,
                retryAfter = decision.Ttl.HasValue ? Math.Max(0, (int)decision.Ttl.Value.TotalSeconds) : (int?)null
            }));
            return;
        }

        await _next(context);
    }

    private async Task<RateLimitConfig?> GetRateLimitConfigAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = ctx.Request.Method.ToUpperInvariant();

        // Refresh Token
        if (path.Contains("/auth/refresh") && method == "POST")
        {
            var key = BuildKey(ctx, "RefreshPolicy");
            return new("RefreshPolicy", 10, TimeSpan.FromMinutes(5), key);
        }

        // OTP (send/verify) ← از PhoneVerificationOptions
        if ((path.Contains("/auth/send-otp") || path.Contains("/auth/verify-otp")) && method == "POST")
        {
            var key = await BuildOtpKeyAsync(ctx, "OtpPolicy"); // شامل phone اگر شد
            var limit = Math.Max(1, _phoneOpts.MaxSendPerWindow);
            var win = TimeSpan.FromSeconds(Math.Max(5, _phoneOpts.WindowSeconds));
            return new("OtpPolicy", limit, win, key);
        }

        // سایر Auth POSTها ← از LoginFlowOptions.RateLimit
        if (path.Contains("/auth/") && method == "POST")
        {
            var key = BuildKey(ctx, "AuthPolicy");
            var limit = Math.Max(1, _loginFlow.RateLimit.Limit);
            var win = TimeSpan.FromSeconds(Math.Max(5, _loginFlow.RateLimit.WindowSeconds));
            return new("AuthPolicy", limit, win, key);
        }

        // عمومی API
        if (path.StartsWith("/api/"))
        {
            var key = BuildKey(ctx, "ApiPolicy");
            return new("ApiPolicy", 50, TimeSpan.FromMinutes(1), key);
        }

        return null;
    }

    // Key for general/Auth: uid if auth; else ip+device+ua
    private static string BuildKey(HttpContext ctx, string policy)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceId = ctx.Request.Headers["X-Device-Id"].ToString();
        var dev = string.IsNullOrWhiteSpace(deviceId) ? "nodev" : deviceId;

        if (ctx.User?.Identity?.IsAuthenticated == true)
        {
            var uid = ctx.User.Identity?.Name ?? "unknown";
            if (policy is "AuthPolicy") // کماکان گرانول‌تر برای حملات لاگین
                return $"{policy}:uid:{uid}:ip:{ip}:dev:{dev}";
            return $"{policy}:uid:{uid}";
        }

        var uaHash = Sha256(ctx.Request.Headers.UserAgent.ToString());
        if (policy is "AuthPolicy")
            return $"{policy}:ip:{ip}:dev:{dev}:ua:{uaHash}";
        return $"{policy}:ip:{ip}";
    }

    // Key for OTP: include normalized phone if present (without consuming body)
    private static async Task<string> BuildOtpKeyAsync(HttpContext ctx, string policy)
    {
        var baseKey = BuildKey(ctx, policy);

        // سعی می‌کنیم phone را از JSON body بخوانیم (safe)
        try
        {
            ctx.Request.EnableBuffering();
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            ctx.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("phone", out var phoneEl))
                {
                    var raw = phoneEl.GetString() ?? "";
                    var normalized = NormalizePhone(raw);
                    if (!string.IsNullOrWhiteSpace(normalized))
                        return $"{baseKey}:phone:{normalized}";
                }
            }
        }
        catch { /* ignore */ }

        return baseKey;
    }

    private static string NormalizePhone(string phone)
    {
        // نمونه‌ی ساده: حذف فاصله/خط تیره و جایگزینی 09… به +98…
        var p = new string(phone.Where(char.IsDigit).ToArray());
        if (p.StartsWith("0")) p = "98" + p[1..];
        if (!p.StartsWith("98")) p = "98" + p; // محافظه‌کارانه
        return "+" + p;
    }

    private static string Sha256(string s)
    {
        if (string.IsNullOrEmpty(s)) return "na";
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }

    private sealed record RateLimitConfig(string Policy, int Limit, TimeSpan Window, string Key);
}
