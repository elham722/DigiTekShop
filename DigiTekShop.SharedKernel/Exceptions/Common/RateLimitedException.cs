#nullable enable
using System;
using System.Collections.Generic;
using DigiTekShop.SharedKernel.Errors;

namespace DigiTekShop.SharedKernel.Exceptions.Common;

public sealed class RateLimitedException : DomainException
{
    public static class Meta
    {
        public const string Policy = "policy";
        public const string Key = "key";
        public const string Limit = "limit";
        public const string Remaining = "remaining";
        public const string WindowSeconds = "windowSeconds";
        public const string ResetAtUnix = "resetAtUnix";
        public const string Reason = "reason";
    }

    public RateLimitedException(string? message = null)
        : base(ErrorCodes.Common.RATE_LIMIT_EXCEEDED, message) { }

    public RateLimitedException(
        int limit,
        int remaining,
        TimeSpan window,
        DateTimeOffset resetAt,
        string? policy = null,
        string? key = null,
        string? reason = null,
        string? errorCode = null,             
        Exception? innerException = null)
        : base(
            code: string.IsNullOrWhiteSpace(errorCode) ? ErrorCodes.Common.RATE_LIMIT_EXCEEDED : errorCode!,
            message: reason ?? "Too many requests.",
            innerException: innerException,
            metadata: BuildMetadata(limit, remaining, window, resetAt, policy, key, reason))
    { }

    public static RateLimitedException FromRaw(
        long count,
        int limit,
        TimeSpan window,
        DateTimeOffset resetAt,
        string? policy = null,
        string? key = null,
        string? reason = null,
        string? errorCode = null,
        Exception? innerException = null)
    {
        var remaining = Math.Max(0, limit - (int)count);
        return new RateLimitedException(limit, remaining, window, resetAt, policy, key, reason, errorCode, innerException);
    }

    private static Dictionary<string, object> BuildMetadata(
        int limit,
        int remaining,
        TimeSpan window,
        DateTimeOffset resetAt,
        string? policy,
        string? key,
        string? reason)
    {
        var meta = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Meta.Limit] = limit,
            [Meta.Remaining] = Math.Max(0, remaining),
            [Meta.WindowSeconds] = Math.Max(1, (int)window.TotalSeconds),
            [Meta.ResetAtUnix] = resetAt.ToUnixTimeSeconds()
        };

        if (!string.IsNullOrWhiteSpace(policy)) meta[Meta.Policy] = policy!;
        if (!string.IsNullOrWhiteSpace(key)) meta[Meta.Key] = key!;
        if (!string.IsNullOrWhiteSpace(reason)) meta[Meta.Reason] = reason!;

        return meta;
    }
}
