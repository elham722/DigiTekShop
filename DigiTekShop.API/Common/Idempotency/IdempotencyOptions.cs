namespace DigiTekShop.API.Common.Idempotency;

public sealed class IdempotencyOptions
{
    
    public int TtlHours { get; set; } = 24;

    public int MaxBodySizeBytes { get; set; } = 256 * 1024;

    public string[] AllowedHeaderNames { get; set; } = new[] { "Location", "ETag", "Cache-Control", "Content-Language" };
}