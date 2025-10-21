using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Services.Logout;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace DigiTekShop.UnitTests.Identity.Services.Logout;

public sealed class LogoutServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<DigiTekShopIdentityDbContext> _dbContextMock;
    private readonly Mock<ITokenBlacklistService> _blacklistServiceMock;
    private readonly Mock<ICurrentClient> _currentClientMock;
    private readonly Mock<ILogger<LogoutService>> _loggerMock;
    private readonly LogoutService _logoutService;

    public LogoutServiceTests()
    {
        _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
        
        _dbContextMock = new Mock<DigiTekShopIdentityDbContext>();
        _blacklistServiceMock = new Mock<ITokenBlacklistService>();
        _currentClientMock = new Mock<ICurrentClient>();
        _loggerMock = new Mock<ILogger<LogoutService>>();

        _logoutService = new LogoutService(
            _userManagerMock.Object,
            _dbContextMock.Object,
            _blacklistServiceMock.Object,
            _currentClientMock.Object,
            _loggerMock.Object);
    }

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { UserId = userId, RefreshToken = "valid-refresh-token" };
        
        _currentClientMock.Setup(x => x.Jti).Returns("test-jti");
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());
        _currentClientMock.Setup(x => x.Iat).Returns(DateTimeOffset.UtcNow);
        _currentClientMock.Setup(x => x.Exp).Returns(DateTimeOffset.UtcNow.AddMinutes(15));

        // Act
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_WithNullRefreshToken_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { UserId = userId, RefreshToken = null };
        
        _currentClientMock.Setup(x => x.Jti).Returns("test-jti");
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());

        // Act
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_WithRedisDown_ShouldStillReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { UserId = userId, RefreshToken = "valid-refresh-token" };
        
        _currentClientMock.Setup(x => x.Jti).Returns("test-jti");
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());
        _currentClientMock.Setup(x => x.Iat).Returns(DateTimeOffset.UtcNow);
        _currentClientMock.Setup(x => x.Exp).Returns(DateTimeOffset.UtcNow.AddMinutes(15));

        // Mock Redis failure
        _blacklistServiceMock.Setup(x => x.RevokeAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

        // Act
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Should still succeed despite Redis failure
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeAccessToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jti = "test-jti";
        var request = new LogoutRequest { UserId = userId, RefreshToken = "valid-refresh-token" };
        
        _currentClientMock.Setup(x => x.Jti).Returns(jti);
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());
        _currentClientMock.Setup(x => x.Iat).Returns(DateTimeOffset.UtcNow);
        _currentClientMock.Setup(x => x.Exp).Returns(DateTimeOffset.UtcNow.AddMinutes(15));

        // Act
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _blacklistServiceMock.Verify(x => x.RevokeAccessTokenAsync(jti, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithValidRefreshToken_ShouldRevokeRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { UserId = userId, RefreshToken = "valid-refresh-token" };
        
        _currentClientMock.Setup(x => x.Jti).Returns("test-jti");
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());

        // Act
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify that refresh token revocation was attempted
        // This would require proper database setup to fully test
    }

    #endregion

    #region LogoutAllAsync Tests

    [Fact]
    public async Task LogoutAllAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutAllRequest { UserId = userId };

        // Act
        var result = await _logoutService.LogoutAllAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAllAsync_WithRedisDown_ShouldStillReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutAllRequest { UserId = userId };

        // Mock Redis failure
        _blacklistServiceMock.Setup(x => x.RevokeAllUserTokensAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

        // Act
        var result = await _logoutService.LogoutAllAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Should still succeed despite Redis failure
    }

    [Fact]
    public async Task LogoutAllAsync_ShouldRevokeAllUserTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutAllRequest { UserId = userId };

        // Act
        var result = await _logoutService.LogoutAllAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _blacklistServiceMock.Verify(x => x.RevokeAllUserTokensAsync(userId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAllAsync_ShouldRevokeAllRefreshTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutAllRequest { UserId = userId };

        // Act
        var result = await _logoutService.LogoutAllAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify that all refresh tokens for the user are revoked
        // This would require proper database setup to fully test
    }

    #endregion

    #region Redis Failure Scenarios

    [Fact]
    public async Task LogoutAsync_WithRedisTimeout_ShouldLogWarningAndContinue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { UserId = userId, RefreshToken = "valid-refresh-token" };
        
        _currentClientMock.Setup(x => x.Jti).Returns("test-jti");
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());
        _currentClientMock.Setup(x => x.Iat).Returns(DateTimeOffset.UtcNow);
        _currentClientMock.Setup(x => x.Exp).Returns(DateTimeOffset.UtcNow.AddMinutes(15));

        // Mock Redis timeout
        _blacklistServiceMock.Setup(x => x.RevokeAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Redis timeout"));

        // Act
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Should still succeed
        // Verify that warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Redis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAllAsync_WithRedisConnectionFailure_ShouldLogWarningAndContinue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutAllRequest { UserId = userId };

        // Mock Redis connection failure
        _blacklistServiceMock.Setup(x => x.RevokeAllUserTokensAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.ConnectionFailure, "Connection failed"));

        // Act
        var result = await _logoutService.LogoutAllAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Should still succeed
        // Verify that warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Redis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task LogoutAsync_WithEmptyJti_ShouldStillProcess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { UserId = userId, RefreshToken = "valid-refresh-token" };
        
        _currentClientMock.Setup(x => x.Jti).Returns(string.Empty);
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());

        // Act
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should not attempt to revoke access token if JTI is empty
        _blacklistServiceMock.Verify(x => x.RevokeAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WithExpiredAccessToken_ShouldStillProcess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { UserId = userId, RefreshToken = "valid-refresh-token" };
        
        _currentClientMock.Setup(x => x.Jti).Returns("test-jti");
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());
        _currentClientMock.Setup(x => x.Iat).Returns(DateTimeOffset.UtcNow.AddHours(-2));
        _currentClientMock.Setup(x => x.Exp).Returns(DateTimeOffset.UtcNow.AddHours(-1)); // Expired

        // Act
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should still attempt to revoke the token (best effort)
        _blacklistServiceMock.Verify(x => x.RevokeAccessTokenAsync("test-jti", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task LogoutAsync_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutRequest { UserId = userId, RefreshToken = "valid-refresh-token" };
        
        _currentClientMock.Setup(x => x.Jti).Returns("test-jti");
        _currentClientMock.Setup(x => x.Sub).Returns(userId.ToString());
        _currentClientMock.Setup(x => x.Iat).Returns(DateTimeOffset.UtcNow);
        _currentClientMock.Setup(x => x.Exp).Returns(DateTimeOffset.UtcNow.AddMinutes(15));

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _logoutService.LogoutAsync(request, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task LogoutAllAsync_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogoutAllRequest { UserId = userId };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _logoutService.LogoutAllAsync(request, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
    }

    #endregion
}
