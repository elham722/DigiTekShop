using System.Net;
using DigiTekShop.API.IntegrationTests.Factories;
using DigiTekShop.API.IntegrationTests.Helpers;
using FluentAssertions;

namespace DigiTekShop.API.IntegrationTests.RateLimiting;

/// <summary>
/// تست‌های شکل و سازگاری هدرهای Rate Limit
/// </summary>
public sealed class HeaderShapeTests : IClassFixture<ApiFactoryWithRedis>
{
    private readonly ApiFactoryWithRedis _factory;
    private readonly HttpClient _client;

    public HeaderShapeTests(ApiFactoryWithRedis factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Success_Response_Should_Contain_All_Required_Headers()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // هدرهای اجباری
        response.TryGetHeader("X-RateLimit-Limit", out var limit).Should().BeTrue();
        response.TryGetHeader("X-RateLimit-Remaining", out var remaining).Should().BeTrue();
        response.TryGetHeader("X-RateLimit-Reset", out var reset).Should().BeTrue();
        response.TryGetHeader("X-RateLimit-Window", out var window).Should().BeTrue();
        response.TryGetHeader("X-RateLimit-Policy", out var policy).Should().BeTrue();

        // مقادیر نباید خالی باشند
        limit.Should().NotBeNullOrEmpty();
        remaining.Should().NotBeNullOrEmpty();
        reset.Should().NotBeNullOrEmpty();
        window.Should().NotBeNullOrEmpty();
        policy.Should().NotBeNullOrEmpty();

        // Retry-After نباید در 200 باشد
        response.TryGetHeader("Retry-After", out _).Should().BeFalse(
            "Retry-After should not be present on successful requests");
    }

    [Fact]
    public async Task Blocked_Response_Should_Contain_RetryAfter_And_Remaining_Zero()
    {
        // Arrange - پر کردن سقف
        for (int i = 0; i < _factory.Limit; i++)
        {
            await _client.GetAsync("/api/v1/test/ping");
        }

        // Act
        var blockedResponse = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        blockedResponse.StatusCode.Should().Be((HttpStatusCode)429);

        // Remaining باید صفر باشد
        blockedResponse.TryGetHeader("X-RateLimit-Remaining", out var remaining).Should().BeTrue();
        remaining.Should().Be("0");

        // Retry-After باید موجود و مثبت باشد
        var retryAfter = blockedResponse.GetRetryAfter();
        retryAfter.Should().NotBeNull();
        retryAfter!.Value.Should().BeGreaterThan(0);

        // سایر هدرها هم باید موجود باشند
        blockedResponse.TryGetHeader("X-RateLimit-Limit", out _).Should().BeTrue();
        blockedResponse.TryGetHeader("X-RateLimit-Reset", out _).Should().BeTrue();
        blockedResponse.TryGetHeader("X-RateLimit-Window", out _).Should().BeTrue();
        blockedResponse.TryGetHeader("X-RateLimit-Policy", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Limit_Header_Should_Match_Configured_Limit()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        var limit = response.GetRateLimitLimit();
        limit.Should().Be(_factory.Limit, "Limit header should match configured limit");
    }

    [Fact]
    public async Task Window_Header_Should_Match_Configured_Window()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        response.TryGetHeader("X-RateLimit-Window", out var windowStr).Should().BeTrue();
        var window = int.Parse(windowStr);
        window.Should().Be(_factory.WindowSeconds, "Window header should match configured window");
    }

    [Fact]
    public async Task Reset_Header_Should_Be_Valid_Unix_Timestamp()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        var resetTimestamp = response.GetRateLimitReset();
        resetTimestamp.Should().NotBeNull();

        // تبدیل به DateTimeOffset باید معتبر باشد
        var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp!.Value);
        var now = DateTimeOffset.UtcNow;

        resetTime.Should().BeAfter(now.AddSeconds(-10), "Reset time should be in the near future");
        resetTime.Should().BeBefore(now.AddSeconds(_factory.WindowSeconds + 10), 
            "Reset time should be within the window duration");
    }

    [Fact]
    public async Task Policy_Header_Should_Indicate_Applied_Policy()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        response.TryGetHeader("X-RateLimit-Policy", out var policy).Should().BeTrue();
        policy.Should().NotBeNullOrEmpty();
        policy.Should().Be("ApiPolicy", "Test endpoint should use ApiPolicy");
    }

    [Fact]
    public async Task All_Headers_Should_Be_Consistent_Across_Requests()
    {
        // Act - دو درخواست متوالی
        var response1 = await _client.GetAsync("/api/v1/test/ping");
        var response2 = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Limit و Window باید یکسان باشند
        response1.GetRateLimitLimit().Should().Be(response2.GetRateLimitLimit());
        response1.TryGetHeader("X-RateLimit-Window", out var window1);
        response2.TryGetHeader("X-RateLimit-Window", out var window2);
        window1.Should().Be(window2);

        // Remaining در درخواست دوم باید کمتر باشد
        var remaining1 = response1.GetRateLimitRemaining()!.Value;
        var remaining2 = response2.GetRateLimitRemaining()!.Value;
        remaining2.Should().BeLessThan(remaining1);
    }
}

