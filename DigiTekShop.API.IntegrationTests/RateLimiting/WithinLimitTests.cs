using System.Net;
using DigiTekShop.API.IntegrationTests.Factories;
using DigiTekShop.API.IntegrationTests.Helpers;
using FluentAssertions;

namespace DigiTekShop.API.IntegrationTests.RateLimiting;

/// <summary>
/// تست‌های داخل محدوده Rate Limit - باید 200 برگردند و Remaining کاهش یابد
/// </summary>
public sealed class WithinLimitTests : IClassFixture<ApiFactoryWithRedis>
{
    private readonly ApiFactoryWithRedis _factory;
    private readonly HttpClient _client;

    public WithinLimitTests(ApiFactoryWithRedis factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Requests_Under_Limit_Should_Return_200_And_Decrease_Remaining()
    {
        // Arrange
        const int requestCount = 3;

        // Act & Assert
        for (int i = 0; i < requestCount; i++)
        {
            var response = await _client.GetAsync("/api/v1/test/ping");

            // وضعیت باید 200 باشد
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // هدرهای Rate Limit باید موجود باشند
            response.TryGetHeader("X-RateLimit-Limit", out var limitHeader).Should().BeTrue();
            response.TryGetHeader("X-RateLimit-Remaining", out var remainingHeader).Should().BeTrue();

            // Limit باید با مقدار تعریف شده در Factory یکسان باشد
            int.Parse(limitHeader).Should().Be(_factory.Limit);

            // Remaining باید کمتر از Limit باشد
            var remaining = int.Parse(remainingHeader);
            remaining.Should().BeLessThan(_factory.Limit);
        }
    }

    [Fact]
    public async Task Sequential_Requests_Should_Decrease_Remaining_Progressively()
    {
        // Arrange
        int? previousRemaining = null;

        // Act & Assert - درخواست‌های پی در پی باید Remaining را کاهش دهند
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/api/v1/test/ping");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var remaining = response.GetRateLimitRemaining();
            remaining.Should().NotBeNull();

            if (previousRemaining.HasValue)
            {
                // هر درخواست باید Remaining را کاهش دهد
                remaining.Should().BeLessThan(previousRemaining.Value);
            }

            previousRemaining = remaining;
        }
    }

    [Fact]
    public async Task All_Rate_Limit_Headers_Should_Be_Present_On_Success()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // همه هدرهای اصلی Rate Limit باید موجود باشند
        response.TryGetHeader("X-RateLimit-Limit", out _).Should().BeTrue("Limit header is required");
        response.TryGetHeader("X-RateLimit-Remaining", out _).Should().BeTrue("Remaining header is required");
        response.TryGetHeader("X-RateLimit-Reset", out _).Should().BeTrue("Reset header is required");
        response.TryGetHeader("X-RateLimit-Window", out _).Should().BeTrue("Window header is required");
        response.TryGetHeader("X-RateLimit-Policy", out _).Should().BeTrue("Policy header is required");

        // Retry-After نباید در درخواست موفق باشد
        response.TryGetHeader("Retry-After", out _).Should().BeFalse("Retry-After should not be present on successful requests");
    }
}

