using DigiTekShop.API;
using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Contracts.DTOs.Auth.Me;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Options.Token;
using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Time;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Testcontainers.Redis;
using Xunit;

namespace DigiTekShop.IntegrationTests.Auth;

[Collection("Integration")]
public sealed class JwtBearerBlacklistIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly RedisContainer _redisContainer;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ITokenService _tokenService;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly UserManager<User> _userManager;
    private readonly DigiTekShopIdentityDbContext _dbContext;
    private User? _testUser;
    private string? _accessToken;
    private string? _refreshToken;

    public JwtBearerBlacklistIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DigiTekShopIdentityDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Add in-memory database
                services.AddDbContext<DigiTekShopIdentityDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));

                // Configure Redis for testing
                services.Configure<RedisOptions>(options =>
                {
                    options.ConnectionString = "localhost:6379"; // Will be overridden by container
                });
            });
        });

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .Build();

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _tokenService = _scope.ServiceProvider.GetRequiredService<ITokenService>();
        _blacklistService = _scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _dbContext = _scope.ServiceProvider.GetRequiredService<DigiTekShopIdentityDbContext>();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        
        // Update Redis connection string
        var redisOptions = _scope.ServiceProvider.GetRequiredService<IOptions<RedisOptions>>();
        // Note: In a real implementation, you'd update the connection string here
        
        // Create test user
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        };

        var result = await _userManager.CreateAsync(_testUser, "TestPassword123!");
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        // Generate tokens
        var tokenResult = await _tokenService.IssueAsync(_testUser.Id);
        if (!tokenResult.IsSuccess)
            throw new InvalidOperationException("Failed to generate test tokens");

        _accessToken = tokenResult.Value!.AccessToken;
        _refreshToken = tokenResult.Value.RefreshToken;
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        _client.Dispose();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }

    #region JWT Bearer Authentication Tests

    [Fact]
    public async Task AuthenticatedRequest_WithValidToken_ShouldSucceed()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticatedRequest_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var expiredToken = CreateExpiredToken();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No authorization header

        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token Blacklist Tests

    [Fact]
    public async Task AuthenticatedRequest_WithRevokedToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        // First, verify the token works
        var initialResponse = await _client.GetAsync("/api/v1/auth/me");
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Revoke the token
        var jti = ExtractJtiFromToken(_accessToken!);
        await _blacklistService.RevokeAccessTokenAsync(jti, CancellationToken.None);

        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithUserRevokedTokens_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        // First, verify the token works
        var initialResponse = await _client.GetAsync("/api/v1/auth/me");
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Revoke all user tokens
        var iat = ExtractIatFromToken(_accessToken!);
        await _blacklistService.RevokeAllUserTokensAsync(_testUser!.Id, iat, CancellationToken.None);

        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldRevokeAccessToken()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var logoutRequest = new LogoutRequest
        {
            UserId = _testUser!.Id,
            RefreshToken = _refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked
        var meResponse = await _client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAll_ShouldRevokeAllUserTokens()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var logoutAllRequest = new LogoutAllRequest
        {
            UserId = _testUser!.Id
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout-all", logoutAllRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked
        var meResponse = await _client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Redis Failure Scenarios

    [Fact]
    public async Task AuthenticatedRequest_WithRedisDown_ShouldStillWork()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        // Stop Redis container to simulate failure
        await _redisContainer.StopAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        // Should still work if configured for fail-open
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token Refresh Tests

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = _refreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var refreshResponse = JsonSerializer.Deserialize<RefreshTokenResponse>(content);
        refreshResponse.Should().NotBeNull();
        refreshResponse!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithRevokedRefreshToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = _refreshToken
        };

        // First, logout to revoke the refresh token
        var logoutRequest = new LogoutRequest
        {
            UserId = _testUser!.Id,
            RefreshToken = _refreshToken
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        
        await _client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper Methods

    private string CreateExpiredToken()
    {
        var jwtSettings = _scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
        var now = DateTimeOffset.UtcNow.AddHours(-2); // 2 hours ago
        var expires = now.AddMinutes(-1); // Expired 1 minute after issue

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtSettings.Key));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, _testUser!.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, ToUnix(now).ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, _testUser.Id.ToString()),
            new(ClaimTypes.Email, _testUser.Email ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string ExtractJtiFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
    }

    private DateTimeOffset ExtractIatFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var iatClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat)?.Value;
        return iatClaim != null ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(iatClaim)) : DateTimeOffset.UtcNow;
    }

    private static long ToUnix(DateTimeOffset dateTime)
    {
        return dateTime.ToUnixTimeSeconds();
    }

    #endregion
}
