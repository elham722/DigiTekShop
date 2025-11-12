namespace DigiTekShop.Identity.UnitTests.Helpers;

/// <summary>
/// Fake ICurrentClient for testing client info
/// </summary>
public sealed class FakeCurrentClient : ICurrentClient
{
    public string? DeviceId { get; set; } = "test-device-123";
    public string? IpAddress { get; set; } = "127.0.0.1";
    public string? UserAgent { get; set; } = "Mozilla/5.0 (Test)";
    public string? AccessTokenRaw { get; set; }
    public string? AccessTokenJti { get; set; }
    public Guid? AccessTokenSubject { get; set; }
    public DateTime? AccessTokenIssuedAtUtc { get; set; }
    public DateTime? AccessTokenExpiresAtUtc { get; set; }
}

