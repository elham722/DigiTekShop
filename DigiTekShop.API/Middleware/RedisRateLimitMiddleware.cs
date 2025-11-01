#nullable enable
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigiTekShop.Contracts.Abstractions.Caching;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.Contracts.Options.Phone;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

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
        // --- Bypass های بدون معنی برای ریت‌لیمیت ---
        var method = context.Request.Method;
        if (HttpMethods.IsOptions(method) || HttpMethods.IsHead(method))
        { await _next(context); return; }

        var pathLower = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (pathLower.StartsWith("/swagger") || pathLower.StartsWith("/health") || pathLower.StartsWith("/favicon"))
        { await _next(context); return; }

        // پالیسی مناسب را پیدا کن
        var cfg = await GetRateLimitConfigAsync(context);
        if (cfg is null)
        { await _next(context); return; }

        // تصمیم ریت‌لیمیت
        var decision = await _rateLimiter.ShouldAllowAsync(cfg.Key, cfg.Limit, cfg.Window, context.RequestAborted);

        // از خودِ decision بخوانیم تا منسجم بماند
        var remaining = Math.Max(0, decision.Limit - (int)decision.Count);

        // هدرهای استاندارد (همیشه ست شوند)
        context.Response.Headers["X-RateLimit-Policy"] = cfg.Policy;
        context.Response.Headers["X-RateLimit-Limit"] = decision.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = decision.ResetAt.ToUnixTimeSeconds().ToString();
        context.Response.Headers["X-RateLimit-Window"] = Math.Max(1, (int)decision.Window.TotalSeconds).ToString();

        if (!decision.Allowed)
        {
            // محاسبه Retry-After (فقط یکبار)
            var retryAfter = Math.Max(0, (int)Math.Ceiling((decision.ResetAt - DateTimeOffset.UtcNow).TotalSeconds));
            context.Response.Headers["Retry-After"] = retryAfter.ToString();

            // Cache-Control: no-store برای جلوگیری از مشکلات Back/Forward
            context.Response.Headers["Cache-Control"] = "no-store, must-revalidate, no-cache";

            // code/type معنادار بر اساس policy
            var errorCode =
                cfg.Policy == "OtpSendPolicy" ? "OTP_SEND_RATE_LIMITED" :
                cfg.Policy == "OtpVerifyPolicy" ? "OTP_VERIFY_RATE_LIMITED" :
                "RATE_LIMIT_EXCEEDED";

            var type = errorCode switch
            {
                "OTP_SEND_RATE_LIMITED" => "urn:problem:otp_send_rate_limited",
                "OTP_VERIFY_RATE_LIMITED" => "urn:problem:otp_verify_rate_limited",
                _ => "urn:problem:rate_limit_exceeded"
            };

            // ProblemDetails استاندارد
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/problem+json";
            var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Type = type,
                Title = "Too Many Requests",
                Status = StatusCodes.Status429TooManyRequests,
                Detail = $"You have exceeded the allowed number of requests. Try again in {retryAfter} seconds.",
                Instance = context.Request.Path
            };
            pd.Extensions["code"] = errorCode;
            pd.Extensions["policy"] = cfg.Policy;
            pd.Extensions["limit"] = decision.Limit;
            pd.Extensions["window"] = Math.Max(1, (int)decision.Window.TotalSeconds);
            pd.Extensions["timestamp"] = DateTimeOffset.UtcNow;

            _logger.LogWarning("429 RateLimit policy={Policy} key={Key} path={Path} limit={Limit} remaining={Remaining} retryAfter={RetryAfter}",
                cfg.Policy, cfg.Key, context.Request.Path, decision.Limit, remaining, retryAfter);

            await context.Response.WriteAsJsonAsync(pd, context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private async Task<RateLimitConfig?> GetRateLimitConfigAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = ctx.Request.Method.ToUpperInvariant();

        // Refresh Token (POST /auth/refresh)
        if (path.Contains("/auth/refresh") && method == "POST")
        {
            var key = BuildKey(ctx, "RefreshPolicy");
            return new("RefreshPolicy", 10, TimeSpan.FromMinutes(5), key);
        }

        // OTP Send (POST /auth/send-otp) - کلید مبتنی بر phone
        if (path.Contains("/auth/send-otp") && method == "POST")
        {
            var key = await BuildOtpSendKeyAsync(ctx, "OtpSendPolicy");
            var limit = Math.Max(1, _phoneOpts.MaxSendPerWindow);
            var win = TimeSpan.FromSeconds(Math.Max(5, _phoneOpts.WindowSeconds));
            return new("OtpSendPolicy", limit, win, key);
        }

        // OTP Verify (POST /auth/verify-otp) - کلید مبتنی بر flowId
        if (path.Contains("/auth/verify-otp") && method == "POST")
        {
            var flowId = await TryReadJsonStringAsync(ctx, "flowId");
            var baseKey = BuildKey(ctx, "OtpVerifyPolicy");
            var key = string.IsNullOrWhiteSpace(flowId) ? baseKey : $"{baseKey}:flow:{flowId}";
            var limit = Math.Max(1, _phoneOpts.MaxVerifyPerWindow);         // ← باید در Options باشد
            var win = TimeSpan.FromSeconds(Math.Max(5, _phoneOpts.VerifyWindowSeconds)); // ← باید در Options باشد
            return new("OtpVerifyPolicy", limit, win, key);
        }

        // سایر Auth POST ها - از LoginFlowOptions.RateLimit
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
            // خواندن از کانفیگ (با مقادیر پیش‌فرض برای تست)
            var limit = ctx.RequestServices.GetRequiredService<IConfiguration>()
                .GetValue<int>("RateLimit:Limit", 10);
            var windowSec = ctx.RequestServices.GetRequiredService<IConfiguration>()
                .GetValue<int>("RateLimit:WindowSeconds", 60);
            var window = TimeSpan.FromSeconds(windowSec);
            return new("ApiPolicy", limit, window, key);
        }

        return null;
    }

    // کلید عمومی/Auth: اگر لاگین است uid، وگرنه ip+device(+ua برای AuthPolicy)
    private static string BuildKey(HttpContext ctx, string policy)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceId = ctx.Request.Headers["X-Device-Id"].ToString();
        var dev = string.IsNullOrWhiteSpace(deviceId) ? "nodev" : deviceId;

        if (ctx.User?.Identity?.IsAuthenticated == true)
        {
            var uid = ctx.User.Identity?.Name ?? "unknown";
            if (policy is "AuthPolicy")
                return $"{policy}:uid:{uid}:ip:{ip}:dev:{dev}";
            return $"{policy}:uid:{uid}";
        }

        var ua = ctx.Request.Headers[HeaderNames.UserAgent].ToString();
        var uaHash = Sha256(ua);

        if (policy is "AuthPolicy")
            return $"{policy}:ip:{ip}:dev:{dev}:ua:{uaHash}";

        return $"{policy}:ip:{ip}";
    }

    // کلید Send-OTP: تلاش برای خواندن phone از JSON body و normalize
    private static async Task<string> BuildOtpSendKeyAsync(HttpContext ctx, string policy)
    {
        var baseKey = BuildKey(ctx, policy);
        try
        {
            var phone = await TryReadJsonStringAsync(ctx, "phone");
            var normalized = NormalizePhone(phone ?? "");
            if (!string.IsNullOrWhiteSpace(normalized))
                return $"{baseKey}:phone:{normalized}";
        }
        catch { /* ignore */ }
        return baseKey;
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return string.Empty;
        if (digits.StartsWith("0")) digits = "98" + digits[1..];
        if (!digits.StartsWith("98")) digits = "98" + digits; // محافظه‌کارانه
        return "+" + digits;
    }

    private static async Task<string?> TryReadJsonStringAsync(HttpContext ctx, string prop)
    {
        try
        {
            ctx.Request.EnableBuffering();
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            ctx.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty(prop, out var el))
                    return el.GetString();
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private static string Sha256(string s)
    {
        if (string.IsNullOrEmpty(s)) return "na";
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }

    private sealed record RateLimitConfig(string Policy, int Limit, TimeSpan Window, string Key);
}
