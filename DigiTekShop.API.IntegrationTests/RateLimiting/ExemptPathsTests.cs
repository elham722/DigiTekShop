using System.Net;
using DigiTekShop.API.IntegrationTests.Factories;
using FluentAssertions;

namespace DigiTekShop.API.IntegrationTests.RateLimiting;

/// <summary>
/// تست‌های مسیرهای معاف از Rate Limit - مانند health checks و swagger
/// </summary>
public sealed class ExemptPathsTests : IClassFixture<ApiFactoryWithRedis>
{
    private readonly ApiFactoryWithRedis _factory;

    public ExemptPathsTests(ApiFactoryWithRedis factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/favicon.ico")]
    public async Task Exempt_Paths_Should_Not_Be_RateLimited(string path)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - درخواست‌های زیاد (بیشتر از Limit)
        for (int i = 0; i < _factory.Limit * 2; i++)
        {
            var response = await client.GetAsync(path);

            // Assert - هیچکدام نباید 429 باشند
            // ممکن است 404 باشند (مثلاً swagger در تست) ولی نباید 429 باشند
            ((int)response.StatusCode).Should().NotBe(429, 
                $"Exempt path {path} should not be rate limited");
        }
    }

    [Fact]
    public async Task OPTIONS_Requests_Should_Not_Be_RateLimited()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - درخواست‌های OPTIONS زیاد
        for (int i = 0; i < _factory.Limit * 2; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/test/ping");
            var response = await client.SendAsync(request);

            // Assert
            ((int)response.StatusCode).Should().NotBe(429, "OPTIONS requests should not be rate limited");
        }
    }

    [Fact]
    public async Task HEAD_Requests_Should_Not_Be_RateLimited()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - درخواست‌های HEAD زیاد
        for (int i = 0; i < _factory.Limit * 2; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, "/api/v1/test/ping");
            var response = await client.SendAsync(request);

            // Assert
            ((int)response.StatusCode).Should().NotBe(429, "HEAD requests should not be rate limited");
        }
    }
}

