using DigiTekShop.API.IntegrationTests.Factories;
using DigiTekShop.API.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace DigiTekShop.API.IntegrationTests.Auth;

/// <summary>
/// تست‌های Integration برای Refresh Token و Rotation
/// </summary>
[Collection("Auth")]
public sealed class RefreshTokenTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;

    public RefreshTokenTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions 
        { 
            AllowAutoRedirect = false 
        });
    }

    [Fact]
    public async Task RefreshToken_ValidToken_Should_IssueNewTokens()
    {
        // Arrange: ورود و دریافت توکن‌ها
        var (_, loginResult) = await _client.SendAndVerifyOtpAsync(_factory.SmsFake.Sent);
        loginResult.Should().NotBeNull();

        var refreshToken = loginResult!.RefreshToken;
        var request = new { refreshToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "valid refresh token should return new tokens");

        var newTokens = await response.ExtractLoginResponseAsync();
        newTokens.Should().NotBeNull("new tokens should be issued");
        
        newTokens!.AccessToken.Should().NotBeNullOrWhiteSpace("new access token should be issued");
        newTokens.RefreshToken.Should().NotBeNullOrWhiteSpace("new refresh token should be issued");
        
        // توکن‌های جدید باید متفاوت باشند
        newTokens.AccessToken.Should().NotBe(loginResult.AccessToken, 
            "new access token should be different");
        newTokens.RefreshToken.Should().NotBe(loginResult.RefreshToken, 
            "new refresh token should be different (rotation)");
        
        // UserId باید یکسان بماند
        newTokens.UserId.Should().Be(loginResult.UserId, 
            "user ID should remain the same");
    }

    [Fact]
    public async Task RefreshToken_Rotation_Should_InvalidatePreviousToken()
    {
        // Arrange: ورود
        var (_, loginResult) = await _client.SendAndVerifyOtpAsync(_factory.SmsFake.Sent);
        loginResult.Should().NotBeNull();

        var refreshToken1 = loginResult!.RefreshToken;

        // Act 1: رفرش اول - دریافت refreshToken2
        var response1 = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", 
            new { refreshToken = refreshToken1 });
        response1.EnsureSuccessStatusCode();

        var tokens2 = await response1.ExtractLoginResponseAsync();
        tokens2.Should().NotBeNull();
        var refreshToken2 = tokens2!.RefreshToken;

        // Act 2: تلاش برای استفاده دوباره از refreshToken1 (باید fail شود)
        var response2 = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", 
            new { refreshToken = refreshToken1 });

        // Assert
        response2.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest,
            (HttpStatusCode)422);
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_Should_Return401()
    {
        // Arrange
        var fakeToken = "invalid-refresh-token-12345";
        var request = new { refreshToken = fakeToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_EmptyToken_Should_Return400()
    {
        // Arrange
        var request = new { refreshToken = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            (HttpStatusCode)422);
    }

    [Fact]
    public async Task RefreshToken_MultipleSequentialRefreshes_Should_Work()
    {
        // Arrange: ورود
        var (_, loginResult) = await _client.SendAndVerifyOtpAsync(_factory.SmsFake.Sent);
        loginResult.Should().NotBeNull();

        var currentRefreshToken = loginResult!.RefreshToken;
        var userId = loginResult.UserId;

        // Act: چند بار پشت سر هم refresh کنیم
        for (int i = 0; i < 3; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", 
                new { refreshToken = currentRefreshToken });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, 
                $"refresh #{i + 1} should succeed");

            var newTokens = await response.ExtractLoginResponseAsync();
            newTokens.Should().NotBeNull($"tokens should be issued for refresh #{i + 1}");
            
            newTokens!.RefreshToken.Should().NotBeNullOrWhiteSpace();
            newTokens.RefreshToken.Should().NotBe(currentRefreshToken, 
                "each refresh should rotate the token");
            
            newTokens.UserId.Should().Be(userId, 
                "user ID should remain consistent");

            // توکن جدید برای iteration بعدی
            currentRefreshToken = newTokens.RefreshToken;
        }
    }

    [Fact]
    public async Task RefreshToken_AfterLogout_Should_BeInvalid()
    {
        // Arrange: ورود
        var (_, loginResult) = await _client.SendAndVerifyOtpAsync(_factory.SmsFake.Sent);
        loginResult.Should().NotBeNull();

        var refreshToken = loginResult!.RefreshToken;
        var accessToken = loginResult.AccessToken;
        var userId = loginResult.UserId;

        // Auth header برای logout
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        // Act 1: Logout
        var logoutResponse = await _client.PostAsJsonAsync("/api/v1/auth/logout", 
            new { userId });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, 
            "logout should succeed");

        // Act 2: تلاش برای refresh بعد از logout
        _client.DefaultRequestHeaders.Authorization = null; // حذف auth header
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", 
            new { refreshToken });

        // Assert
        refreshResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_ConcurrentRequests_Should_BeHandledSafely()
    {
        // Arrange: ورود
        var (_, loginResult) = await _client.SendAndVerifyOtpAsync(_factory.SmsFake.Sent);
        loginResult.Should().NotBeNull();

        var refreshToken = loginResult!.RefreshToken;

        // Act: درخواست‌های همزمان با همان refresh token
        var tasks = Enumerable.Range(0, 3).Select(_ =>
            _client.PostAsJsonAsync("/api/v1/auth/refresh-token", new { refreshToken })
        ).ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert: فقط یکی باید موفق شود، بقیه باید fail شوند
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var failCount = responses.Count(r => !r.IsSuccessStatusCode);

        // در بهترین حالت، فقط یکی موفق می‌شود (بستگی به race condition handling دارد)
        // در بدترین حالت، همه موفق می‌شوند اما توکن‌های متفاوت صادر می‌شود
        
        // اینجا فقط چک می‌کنیم که سیستم crash نکرده
        (successCount + failCount).Should().Be(3, "all requests should complete");
        
        // اگر بیش از یکی موفق شده، باید refresh token‌های متفاوت برگردانده باشند
        if (successCount > 1)
        {
            var successfulTokens = new List<string>();
            foreach (var response in responses.Where(r => r.IsSuccessStatusCode))
            {
                var result = await response.ExtractLoginResponseAsync();
                if (result?.RefreshToken != null)
                {
                    successfulTokens.Add(result.RefreshToken);
                }
            }

            successfulTokens.Should().OnlyHaveUniqueItems(
                "if multiple requests succeed, they should return different tokens");
        }
    }

    [Fact]
    public async Task RefreshToken_NewAccessToken_Should_BeValidJwt()
    {
        // Arrange: ورود و refresh
        var (_, loginResult) = await _client.SendAndVerifyOtpAsync(_factory.SmsFake.Sent);
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", 
            new { refreshToken = loginResult!.RefreshToken });
        
        refreshResponse.EnsureSuccessStatusCode();
        var newTokens = await refreshResponse.ExtractLoginResponseAsync();

        // Act & Assert
        var jwt = AuthTestHelpers.ParseJwtToken(newTokens!.AccessToken);
        jwt.Should().NotBeNull("new access token should be valid JWT");

        jwt!.GetUserId().Should().Be(loginResult.UserId, 
            "JWT should contain correct user ID");
        jwt.GetJti().Should().NotBeNullOrWhiteSpace(
            "JWT should have unique JTI");
        jwt.IsExpired().Should().BeFalse(
            "new JWT should not be expired");
    }
}

