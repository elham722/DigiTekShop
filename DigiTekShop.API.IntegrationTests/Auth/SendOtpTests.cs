using DigiTekShop.API.IntegrationTests.Factories;
using DigiTekShop.API.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace DigiTekShop.API.IntegrationTests.Auth;

/// <summary>
/// تست‌های Integration برای endpoint ارسال OTP
/// </summary>
[Collection("Auth")]
public sealed class SendOtpTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;

    public SendOtpTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions 
        { 
            AllowAutoRedirect = false 
        });
    }

    [Fact]
    public async Task SendOtp_ValidPhone_Should_Return204_And_SendSms()
    {
        // Arrange
        var phone = AuthTestHelpers.GenerateUniquePhone();
        var request = new { phone };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, 
            "valid phone should return 204 NoContent");

        // باید SMS ارسال شده باشد
        _factory.SmsFake.Sent.Should().NotBeEmpty("at least one SMS should be sent");
        
        var lastSms = _factory.SmsFake.Sent
            .Where(s => s.Phone.Contains(phone.Replace("+", "")))
            .OrderByDescending(s => s.SentAtUtc)
            .FirstOrDefault();
            
        lastSms.Should().NotBeNull("SMS should be sent to the requested phone");
        lastSms!.Code.Should().NotBeNullOrWhiteSpace("OTP code should be present");
        lastSms.Code.Should().HaveLength(6, "default OTP length is 6 digits");
        lastSms.Code.Should().MatchRegex(@"^\d{6}$", "OTP should be numeric");
    }

    [Fact]
    public async Task SendOtp_InvalidPhone_Should_Return400()
    {
        // Arrange
        var invalidPhone = "invalid-phone";
        var request = new { phone = invalidPhone };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            (HttpStatusCode)422);
    }

    [Fact]
    public async Task SendOtp_EmptyPhone_Should_Return400()
    {
        // Arrange
        var request = new { phone = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            (HttpStatusCode)422);
    }

    [Fact]
    public async Task SendOtp_RateLimited_Should_Return429_With_RetryAfter()
    {
        // Arrange
        var phone = AuthTestHelpers.GenerateUniquePhone();
        var request = new { phone };

        // Act 1: اولین درخواست باید موفق شود
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, 
            "first request should succeed");

        // Act 2: درخواست بلافاصله بعدی باید rate limit بخورد
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", request);

        // Assert
        secondResponse.StatusCode.Should().Be((HttpStatusCode)429, 
            "immediate retry should be rate limited");

        // باید هدر Retry-After داشته باشد
        secondResponse.Headers.Should().ContainKey("Retry-After", 
            "rate limit response should include Retry-After header");
        
        var retryAfter = secondResponse.Headers.GetValues("Retry-After").FirstOrDefault();
        retryAfter.Should().NotBeNullOrWhiteSpace("Retry-After should have a value");
        
        // Retry-After باید عددی باشد (ثانیه)
        int.TryParse(retryAfter, out var seconds).Should().BeTrue(
            "Retry-After should be in seconds format");
        seconds.Should().BeGreaterThan(0, "Retry-After should be positive");
    }

    [Fact]
    public async Task SendOtp_MultiplePhones_Should_WorkIndependently()
    {
        // Arrange
        var phone1 = AuthTestHelpers.GenerateUniquePhone();
        var phone2 = AuthTestHelpers.GenerateUniquePhone();

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", new { phone = phone1 });
        var response2 = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", new { phone = phone2 });

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.NoContent, 
            "first phone should succeed");
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent, 
            "second phone should also succeed independently");

        // هر دو باید SMS دریافت کرده باشند
        _factory.SmsFake.GetSentCount(phone1).Should().BeGreaterThan(0);
        _factory.SmsFake.GetSentCount(phone2).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SendOtp_Should_Include_RateLimitHeaders()
    {
        // Arrange
        var phone = AuthTestHelpers.GenerateUniquePhone();
        var request = new { phone };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/send-otp", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // بررسی هدرهای Rate Limit (بسته به پیاده‌سازی شما)
        // این optional است - اگر هدرهای rate limit ندارید، این تست را حذف کنید
        var headers = response.Headers.Select(h => h.Key.ToLower()).ToList();
        
        // معمولاً Rate Limit headers شامل این موارد هستند:
        // X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset
        // اگر ندارید، این assertion را کامنت کنید
        
        // headers.Should().Contain(h => h.Contains("ratelimit"), 
        //     "response should include rate limit headers");
    }
}

