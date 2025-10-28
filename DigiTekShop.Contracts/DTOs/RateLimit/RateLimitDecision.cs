namespace DigiTekShop.Contracts.DTOs.RateLimit;
public readonly record struct RateLimitDecision(bool Allowed, long Count, TimeSpan? Ttl);
