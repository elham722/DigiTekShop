using DigiTekShop.Contracts.DTOs.RateLimit;
using static DigiTekShop.SharedKernel.Http.RateLimitMetaKeys;

namespace DigiTekShop.SharedKernel.Results;

public static class RateLimitResultExtensions
{
    public static Result WithRateLimit(
        this Result r,
        RateLimitDecision d,
        string policy,
        string? key = null,
        string? reason = null)
    {
        return r
            .WithMeta(Policy, policy)
            .WithMeta(Key, key ?? "")
            .WithMeta(Limit, d.Limit)
            .WithMeta(Remaining, Math.Max(0, d.Limit - (int)d.Count))
            .WithMeta(WindowSeconds, Math.Max(1, (int)d.Window.TotalSeconds))
            .WithMeta(ResetAtUnix, d.ResetAt.ToUnixTimeSeconds())
            .WithMeta(Reason, reason ?? "");
    }

    public static Result<T> WithRateLimit<T>(
        this Result<T> r,
        RateLimitDecision d,
        string policy,
        string? key = null,
        string? reason = null)
    {
        return r
            .WithMeta(Policy, policy)
            .WithMeta(Key, key ?? "")
            .WithMeta(Limit, d.Limit)
            .WithMeta(Remaining, Math.Max(0, d.Limit - (int)d.Count))
            .WithMeta(WindowSeconds, Math.Max(1, (int)d.Window.TotalSeconds))
            .WithMeta(ResetAtUnix, d.ResetAt.ToUnixTimeSeconds())
            .WithMeta(Reason, reason ?? "");
    }
}