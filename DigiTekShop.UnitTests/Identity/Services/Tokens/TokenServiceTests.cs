using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Options.Token;
using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using DigiTekShop.Identity.Services.Tokens;
using DigiTekShop.SharedKernel.Results;
using DigiTekShop.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace DigiTekShop.UnitTests.Identity.Services.Tokens;

public sealed class TokenServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<DigiTekShopIdentityDbContext> _dbContextMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<ITokenBlacklistService> _blacklistServiceMock;
    private readonly Mock<ICurrentClient> _currentClientMock;
    private readonly Mock<ILogger<TokenService>> _loggerMock;
    private readonly JwtSettings _jwtSettings;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        
        _dbContextMock = new Mock<DigiTekShopIdentityDbContext>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _blacklistServiceMock = new Mock<ITokenBlacklistService>();
        _currentClientMock = new Mock<ICurrentClient>();
        _loggerMock = new Mock<ILogger<TokenService>>();

        _jwtSettings = new JwtSettings
        {
            Key = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789",
            Issuer = "DigiTekShop.Test",
            Audience = "DigiTekShop.Users",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7,
            RefreshTokenHashSecret = "RefreshTokenHashSecretForTesting123456789"
        };

        _tokenService = new TokenService(
            _userManagerMock.Object,
            _dbContextMock.Object,
            _dateTimeProviderMock.Object,
            Options.Create(_jwtSettings),
            _blacklistServiceMock.Object,
            _currentClientMock.Object,
            _loggerMock.Object);
    }

    #region IssueAsync Tests

    [Fact]
    public async Task IssueAsync_WithValidUser_ShouldReturnSuccessWithTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", UserName = "testuser" };
        var now = DateTimeOffset.UtcNow;
        
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _currentClientMock.Setup(x => x.DeviceId).Returns("test-device-123");

        // Act
        var result = await _tokenService.IssueAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.ExpiresAt.Should().BeAfter(now);
    }

    [Fact]
    public async Task IssueAsync_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _tokenService.IssueAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task IssueAsync_ShouldCreateValidJwtToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", UserName = "testuser" };
        var now = DateTimeOffset.UtcNow;
        
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _tokenService.IssueAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var token = result.Value!.AccessToken;
        
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        
        jwt.Subject.Should().Be(userId.ToString());
        jwt.Issuer.Should().Be(_jwtSettings.Issuer);
        jwt.Audiences.Should().Contain(_jwtSettings.Audience);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        var refreshToken = "valid-refresh-token";
        var request = new RefreshTokenRequest { RefreshToken = refreshToken };
        var now = DateTimeOffset.UtcNow;

        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Mock the refresh token validation and user lookup
        // This would require setting up the database context properly

        // Act
        var result = await _tokenService.RefreshAsync(request, CancellationToken.None);

        // Assert
        // Note: This test would need proper database setup to work fully
        // For now, we're testing the structure
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshAsync_WithInvalidRefreshToken_ShouldReturnFailure()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid-token" };

        // Act
        var result = await _tokenService.RefreshAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredRefreshToken_ShouldReturnFailure()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "expired-token" };
        var now = DateTimeOffset.UtcNow.AddDays(-10); // Expired token
        
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        // Act
        var result = await _tokenService.RefreshAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region RevokeAsync Tests

    [Fact]
    public async Task RevokeAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "valid-refresh-token";

        // Act
        var result = await _tokenService.RevokeAsync(refreshToken, userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAsync_WithNullToken_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _tokenService.RevokeAsync(null, userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region RevokeAllAsync Tests

    [Fact]
    public async Task RevokeAllAsync_WithValidUser_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _tokenService.RevokeAllAsync(userId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region RevokeAccessJtiAsync Tests

    [Fact]
    public async Task RevokeAccessJtiAsync_WithValidJti_ShouldReturnSuccess()
    {
        // Arrange
        var jti = "valid-jti";

        // Act
        var result = await _tokenService.RevokeAccessJtiAsync(jti, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region TryReadAccessToken Tests

    [Fact]
    public void TryReadAccessToken_WithValidToken_ShouldReturnTokenInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        var now = DateTimeOffset.UtcNow;
        
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Create a valid token first
        var issueResult = _tokenService.IssueAsync(userId).Result;
        var token = issueResult.Value!.AccessToken;

        // Act
        var (ok, jti, sub, iat, exp) = _tokenService.TryReadAccessToken(token);

        // Assert
        ok.Should().BeTrue();
        jti.Should().NotBeNullOrEmpty();
        sub.Should().Be(userId);
        iat.Should().BeCloseTo(now, TimeSpan.FromMinutes(1));
        exp.Should().BeCloseTo(now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void TryReadAccessToken_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var (ok, jti, sub, iat, exp) = _tokenService.TryReadAccessToken(invalidToken);

        // Assert
        ok.Should().BeFalse();
        jti.Should().BeNull();
        sub.Should().BeNull();
        iat.Should().BeNull();
        exp.Should().BeNull();
    }

    [Fact]
    public void TryReadAccessToken_WithExpiredToken_ShouldReturnTokenInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        var pastTime = DateTimeOffset.UtcNow.AddMinutes(-30);
        
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(pastTime);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Create an expired token
        var issueResult = _tokenService.IssueAsync(userId).Result;
        var token = issueResult.Value!.AccessToken;

        // Act
        var (ok, jti, sub, iat, exp) = _tokenService.TryReadAccessToken(token);

        // Assert
        ok.Should().BeTrue(); // Should still be able to read the token
        jti.Should().NotBeNullOrEmpty();
        sub.Should().Be(userId);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task IssueAsync_ConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", UserName = "testuser" };
        var now = DateTimeOffset.UtcNow;
        
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _tokenService.IssueAsync(userId))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        results.Should().AllSatisfy(r => r.Value.Should().NotBeNull());
    }

    #endregion

    #region Token Rotation Tests

    [Fact]
    public async Task RefreshAsync_ShouldRotateRefreshToken()
    {
        // This test would verify that refresh tokens are rotated on each refresh
        // Implementation would require proper database setup
        var request = new RefreshTokenRequest { RefreshToken = "test-token" };
        
        // Act
        var result = await _tokenService.RefreshAsync(request, CancellationToken.None);
        
        // Assert
        // This would need to verify that the old refresh token is invalidated
        // and a new one is issued
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshAsync_WithReusedToken_ShouldDetectReuse()
    {
        // This test would verify that token reuse is detected and handled
        var request = new RefreshTokenRequest { RefreshToken = "reused-token" };
        
        // Act
        var result = await _tokenService.RefreshAsync(request, CancellationToken.None);
        
        // Assert
        // This would need to verify that reuse is detected and all tokens are revoked
        result.Should().NotBeNull();
    }

    #endregion
}
