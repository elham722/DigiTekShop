using System.Net;
using DigiTekShop.API.IntegrationTests.Factories;
using DigiTekShop.API.IntegrationTests.Helpers;
using FluentAssertions;

namespace DigiTekShop.API.IntegrationTests.RateLimiting;

/// <summary>
/// تست‌های همزمانی - بدون overcount یا خطای 5xx
/// </summary>
public sealed class ConcurrencyTests : IClassFixture<ApiFactoryWithRedis>
{
    private readonly ApiFactoryWithRedis _factory;

    public ConcurrencyTests(ApiFactoryWithRedis factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Parallel_Requests_Should_Cap_At_Limit_Without_5xx()
    {
        // Arrange
        var client = _factory.CreateClient();
        var totalRequests = _factory.Limit + 10;

        // Act - ارسال همزمان درخواست‌ها
        var tasks = Enumerable
            .Range(0, totalRequests)
            .Select(_ => client.GetAsync("/api/v1/test/ping"))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var blockedCount = results.Count(r => (int)r.StatusCode == 429);
        var errorCount = results.Count(r => (int)r.StatusCode >= 500);

        // تعداد موفق نباید از Limit بیشتر باشد
        successCount.Should().BeLessThanOrEqualTo(_factory.Limit, 
            "Success count should not exceed the rate limit");

        // مجموع موفق + بلاک باید برابر کل درخواست‌ها باشد
        (successCount + blockedCount).Should().Be(totalRequests, 
            "All requests should either succeed or be rate limited");

        // هیچ خطای 5xx نباید رخ دهد
        errorCount.Should().Be(0, 
            "No 5xx errors should occur during concurrent rate limiting");
    }

    [Fact]
    public async Task Concurrent_Requests_Should_Not_Overcount()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - ارسال دقیقاً به اندازه Limit به صورت همزمان
        var tasks = Enumerable
            .Range(0, _factory.Limit)
            .Select(_ => client.GetAsync("/api/v1/test/ping"))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - همه باید موفق باشند (نباید overcount شود)
        var successCount = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        successCount.Should().Be(_factory.Limit, 
            "All requests up to the limit should succeed in concurrent scenario");

        // درخواست بعدی باید بلاک شود
        var nextResponse = await client.GetAsync("/api/v1/test/ping");
        nextResponse.StatusCode.Should().Be((HttpStatusCode)429, 
            "Next request after concurrent batch should be rate limited");
    }

    [Fact]
    public async Task Race_Condition_Should_Not_Cause_Negative_Remaining()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - ارسال بیشتر از Limit
        var tasks = Enumerable
            .Range(0, _factory.Limit + 5)
            .Select(_ => client.GetAsync("/api/v1/test/ping"))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - هیچ Remaining منفی نباید وجود داشته باشد
        foreach (var response in results)
        {
            if (response.TryGetHeader("X-RateLimit-Remaining", out var remainingStr))
            {
                var remaining = int.Parse(remainingStr);
                remaining.Should().BeGreaterThanOrEqualTo(0, 
                    "Remaining should never be negative even under race conditions");
            }
        }
    }
}

