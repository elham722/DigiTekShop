using System.Net;
using DigiTekShop.API.IntegrationTests.Factories;
using DigiTekShop.API.IntegrationTests.Helpers;
using FluentAssertions;

namespace DigiTekShop.API.IntegrationTests.RateLimiting;

/// <summary>
/// تست‌های تجاوز از محدوده Rate Limit - باید 429 برگردند با Retry-After
/// </summary>
public sealed class ExceedingLimitTests : IClassFixture<ApiFactoryWithRedis>
{
    private readonly ApiFactoryWithRedis _factory;
    private readonly HttpClient _client;

    public ExceedingLimitTests(ApiFactoryWithRedis factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Exceeding_Limit_Should_Return_429_With_RetryAfter()
    {
        // Arrange - پر کردن سقف
        for (int i = 0; i < _factory.Limit; i++)
        {
            var response = await _client.GetAsync("/api/v1/test/ping");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Act - درخواست بعدی باید بلاک شود
        var blockedResponse = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        blockedResponse.StatusCode.Should().Be((HttpStatusCode)429);

        // Remaining باید صفر باشد
        var remaining = blockedResponse.GetRateLimitRemaining();
        remaining.Should().Be(0, "Remaining should be 0 when rate limited");

        // Retry-After باید موجود باشد
        var retryAfter = blockedResponse.GetRetryAfter();
        retryAfter.Should().NotBeNull("Retry-After header is required on 429");
        retryAfter!.Value.Should().BeGreaterThan(0, "Retry-After should be positive");
    }

    [Fact]
    public async Task Multiple_Requests_After_Limit_Should_All_Return_429()
    {
        // Arrange - پر کردن سقف
        for (int i = 0; i < _factory.Limit; i++)
        {
            await _client.GetAsync("/api/v1/test/ping");
        }

        // Act - چندین درخواست اضافی
        var blockedResponses = new List<HttpResponseMessage>();
        for (int i = 0; i < 3; i++)
        {
            blockedResponses.Add(await _client.GetAsync("/api/v1/test/ping"));
        }

        // Assert - همه باید 429 باشند
        blockedResponses.Should().AllSatisfy(response =>
        {
            response.StatusCode.Should().Be((HttpStatusCode)429);
            response.GetRateLimitRemaining().Should().Be(0);
            response.GetRetryAfter().Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Blocked_Response_Should_Return_ProblemDetails()
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
        blockedResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        // خواندن محتوا
        var content = await blockedResponse.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("Too Many Requests");
    }

    [Fact]
    public async Task Rate_Limit_Should_Have_Cache_Control_No_Store()
    {
        // Arrange - پر کردن سقف
        for (int i = 0; i < _factory.Limit; i++)
        {
            await _client.GetAsync("/api/v1/test/ping");
        }

        // Act
        var blockedResponse = await _client.GetAsync("/api/v1/test/ping");

        // Assert
        blockedResponse.TryGetHeader("Cache-Control", out var cacheControl).Should().BeTrue();
        cacheControl.Should().Contain("no-store");
    }
}

