namespace DigiTekShop.SharedKernel.Http;

public static class RateLimitMetaKeys
{
    public const string Policy = "rl.policy";
    public const string Key = "rl.key";
    public const string Limit = "rl.limit";
    public const string Remaining = "rl.remaining";
    public const string WindowSeconds = "rl.windowSeconds";
    public const string ResetAtUnix = "rl.resetAtUnix";
    public const string Reason = "rl.reason";
}