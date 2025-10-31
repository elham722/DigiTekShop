using System.Net;
using DigiTekShop.API.IntegrationTests.Factories;
using DigiTekShop.API.IntegrationTests.Helpers;
using FluentAssertions;

namespace DigiTekShop.API.IntegrationTests.RateLimiting;

/// <summary>
/// تست‌های ریست پنجره Rate Limit - بعد از اتمام Window باید دوباره اجازه دهد
/// </summary>
public sealed class ResetWindowTests : IClassFixture<ApiFactoryWithRedis>
{
    private readonly ApiFactoryWithRedis _factory;
    private readonly HttpClient _client;

    public ResetWindowTests(ApiFactoryWithRedis factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task After_Window_Reset_Should_Allow_Requests_Again()
    {
        // Arrange - پر کردن سقف
        for (int i = 0; i < _factory.Limit; i++)
        {
            var response = await _client.GetAsync("/api/v1/test/ping");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // بلاک شدن
        var blockedResponse = await _client.GetAsync("/api/v1/test/ping");
        blockedResponse.StatusCode.Should().Be((HttpStatusCode)429);

        // خواندن Retry-After
        var retryAfter = blockedResponse.GetRetryAfter();
        retryAfter.Should().NotBeNull();

        // Act - صبر کردن تا ریست + یک بافر کوچک
        await Task.Delay(TimeSpan.FromSeconds(retryAfter!.Value + 2));

        // درخواست دوباره
        var afterResetResponse = await _client.GetAsync("/api/v1/test/ping");

        // Assert - باید دوباره اجازه دهد
        afterResetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Remaining باید بیشتر از صفر باشد
        var remaining = afterResetResponse.GetRateLimitRemaining();
        remaining.Should().NotBeNull();
        remaining!.Value.Should().BeLessThan(_factory.Limit, "After reset, remaining should be less than limit (one request used)");
    }

    [Fact]
    public async Task Reset_Timestamp_Should_Be_In_Future()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resetTimestamp = response.GetRateLimitReset();
        resetTimestamp.Should().NotBeNull("Reset timestamp header should be present");

        var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp!.Value);
        var now = DateTimeOffset.UtcNow;

        resetTime.Should().BeAfter(now, "Reset time should be in the future");
        resetTime.Should().BeCloseTo(now.AddSeconds(_factory.WindowSeconds), TimeSpan.FromSeconds(5));
    }
}

