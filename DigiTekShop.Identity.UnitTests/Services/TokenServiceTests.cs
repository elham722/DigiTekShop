using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Identity.UnitTests.Helpers;

namespace DigiTekShop.Identity.UnitTests.Services;

/// <summary>
/// Unit tests for TokenService - تست‌های کامل برای صدور، تازه‌سازی و لغو توکن‌ها
/// </summary>
public sealed class TokenServiceTests
{
    private readonly FakeDateTimeProvider _timeProvider;
    private readonly FakeCurrentClient _currentClient;
    private readonly ITokenBlacklistService _blacklist;
    private readonly ILogger<TokenService> _logger;
    private readonly JwtSettings _jwtSettings;
    private readonly IOptions<JwtSettings> _jwtOptions;
    private readonly UserManager<User> _userManager;

    public TokenServiceTests()
    {
        _timeProvider = new FakeDateTimeProvider();
        _currentClient = new FakeCurrentClient();
        _blacklist = Substitute.For<ITokenBlacklistService>();
        _logger = Substitute.For<ILogger<TokenService>>();
        
        _jwtSettings = new JwtSettings
        {
            Key = "this-is-a-super-secret-key-for-testing-purposes-minimum-32-characters",
            Issuer = "DigiTekShop.Test",
            Audience = "DigiTekShop.Test.Audience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7,
            RefreshTokenHashSecret = "refresh-token-hash-secret-for-testing-minimum-32-characters"
        };
        _jwtOptions = Options.Create(_jwtSettings);
        
        _userManager = Substitute.For<UserManager<User>>(
            Substitute.For<IUserStore<User>>(),
            null, null, null, null, null, null, null, null);
    }

    #region IssueAsync Tests

    [Fact]
    public async Task IssueAsync_ValidUser_ReturnsSuccessWithTokens()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);

        // Act
        var result = await service.IssueAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.ExpiresIn.Should().Be(60 * 60); // 60 minutes in seconds
    }

    [Fact]
    public async Task IssueAsync_ValidUser_SavesRefreshTokenInDatabase()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);

        // Act
        await service.IssueAsync(user.Id);

        // Assert
        var refreshTokens = await context.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        refreshTokens.Should().HaveCount(1);
        
        var token = refreshTokens[0];
        token.UserId.Should().Be(user.Id);
        token.DeviceId.Should().Be(_currentClient.DeviceId);
        token.CreatedByIp.Should().Be(_currentClient.IpAddress);
        token.UserAgent.Should().Be(_currentClient.UserAgent);
        token.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task IssueAsync_AccessTokenContainsCorrectClaims()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context, "john@test.com");
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);

        // Act
        var result = await service.IssueAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // TryReadAccessToken برای خواندن claims
        var (ok, jti, sub, iat, exp) = service.TryReadAccessToken(result.Value!.AccessToken);
        
        ok.Should().BeTrue();
        jti.Should().NotBeNullOrEmpty();
        sub.Should().Be(user.Id);
        iat.Should().BeCloseTo(_timeProvider.UtcNow, TimeSpan.FromSeconds(5));
        exp.Should().BeCloseTo(_timeProvider.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task IssueAsync_InvalidUserId_ReturnsFailure()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);
        
        var invalidUserId = Guid.NewGuid();
        _userManager.FindByIdAsync(invalidUserId.ToString()).Returns((User?)null);

        // Act
        var result = await service.IssueAsync(invalidUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task IssueAsync_MultipleLoginsSameDevice_RevokesOldTokens()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);

        // Act - اولین login
        var result1 = await service.IssueAsync(user.Id);
        result1.IsSuccess.Should().BeTrue();
        
        // Act - دومین login با همان device
        var result2 = await service.IssueAsync(user.Id);
        result2.IsSuccess.Should().BeTrue();

        // Assert
        var tokens = await context.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        tokens.Should().HaveCount(2);
        
        var oldToken = tokens.First(t => t.CreatedAtUtc < tokens.Max(x => x.CreatedAtUtc));
        oldToken.RevokedAtUtc.Should().NotBeNull();
        oldToken.RevokedReason.Should().Be("new_login");
        
        var newToken = tokens.First(t => t.RevokedAtUtc == null);
        newToken.DeviceId.Should().Be(_currentClient.DeviceId);
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        
        // Issue initial tokens
        var issueResult = await service.IssueAsync(user.Id);
        var refreshToken = issueResult.Value!.RefreshToken;

        // Act
        var result = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshToken }, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBe(refreshToken); // توکن جدید باید متفاوت باشد
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RevokesOldTokenAndCreatesNew()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        
        var issueResult = await service.IssueAsync(user.Id);
        var oldRefreshToken = issueResult.Value!.RefreshToken;

        // Act
        await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = oldRefreshToken }, default);

        // Assert
        var tokens = await context.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        tokens.Should().HaveCount(2);
        
        var revokedToken = tokens.FirstOrDefault(t => t.RevokedAtUtc != null);
        revokedToken.Should().NotBeNull();
        revokedToken!.RevokedReason.Should().Be("rotated");
        revokedToken.ReplacedByTokenId.Should().NotBeNull();
        
        var newToken = tokens.FirstOrDefault(t => t.RevokedAtUtc == null);
        newToken.Should().NotBeNull();
        newToken!.ParentTokenId.Should().Be(revokedToken.Id);
    }

    [Fact]
    public async Task RefreshAsync_TokenAlreadyUsed_ReturnsFailureAndRevokesAllTokens()
    {
        // Arrange - Token Reuse Detection scenario
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        
        var issueResult = await service.IssueAsync(user.Id);
        var refreshToken = issueResult.Value!.RefreshToken;
        
        // اولین refresh (موفق)
        await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshToken }, default);

        // Act - دومین refresh با همان توکن (Token Reuse!)
        var result = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshToken }, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        
        // همه توکن‌های فعال باید revoke شده باشند
        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAtUtc != null)
            .ToListAsync();
        
        tokens.Should().HaveCountGreaterThanOrEqualTo(2);
        tokens.Any(t => t.RevokedReason == "reuse_detected").Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        
        var issueResult = await service.IssueAsync(user.Id);
        var refreshToken = issueResult.Value!.RefreshToken;
        
        // Advance time beyond expiration (7 days + 1)
        _timeProvider.Advance(TimeSpan.FromDays(8));

        // Act
        var result = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshToken }, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = "invalid-token" }, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_EmptyToken_ReturnsValidationFailure()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.RefreshAsync(new RefreshTokenRequest { RefreshToken = "" }, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region RevokeAsync Tests

    [Fact]
    public async Task RevokeAsync_ValidToken_RevokesSuccessfully()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        
        var issueResult = await service.IssueAsync(user.Id);
        var refreshToken = issueResult.Value!.RefreshToken;

        // Act
        var result = await service.RevokeAsync(refreshToken, user.Id, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var token = await context.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);
        token.Should().NotBeNull();
        token!.RevokedAtUtc.Should().NotBeNull();
        token.RevokedReason.Should().Be("manual");
    }

    [Fact]
    public async Task RevokeAsync_TokenNotFound_ReturnsSuccess()
    {
        // Arrange - بر اساس idempotency، revoke توکن نامعتبر success برمی‌گرداند
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.RevokeAsync("non-existent-token", Guid.NewGuid(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAsync_EmptyToken_ReturnsValidationFailure()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.RevokeAsync("", Guid.NewGuid(), default);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region RevokeAllAsync Tests

    [Fact]
    public async Task RevokeAllAsync_RevokesAllActiveTokens()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context);
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        
        // ایجاد 3 توکن فعال
        await service.IssueAsync(user.Id);
        _currentClient.DeviceId = "device-2";
        await service.IssueAsync(user.Id);
        _currentClient.DeviceId = "device-3";
        await service.IssueAsync(user.Id);

        // Act
        var result = await service.RevokeAllAsync(user.Id, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var tokens = await context.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        tokens.Should().HaveCount(3);
        tokens.Should().OnlyContain(t => t.RevokedAtUtc != null);
        tokens.Should().OnlyContain(t => t.RevokedReason == "global_logout");
    }

    [Fact]
    public async Task RevokeAllAsync_NoActiveTokens_ReturnsSuccess()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.RevokeAllAsync(Guid.NewGuid(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region RevokeAccessJtiAsync Tests

    [Fact]
    public async Task RevokeAccessJtiAsync_BlacklistsTokenInRedis()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);
        var jti = Guid.NewGuid().ToString("N");

        // Act
        var result = await service.RevokeAccessJtiAsync(jti, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        await _blacklist.Received(1).RevokeAccessTokenAsync(
            Arg.Is<string>(j => j == jti),
            Arg.Any<DateTime>(),
            Arg.Is<string>(r => r == "manual"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region TryReadAccessToken Tests

    [Fact]
    public async Task TryReadAccessToken_ValidToken_ReadsClaimsCorrectly()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var user = await InMemoryDbHelper.SeedUserAsync(context, "test@example.com");
        
        var service = CreateService(context);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        
        // Issue a real token
        var issueResult = await service.IssueAsync(user.Id);
        var accessToken = issueResult.Value!.AccessToken;

        // Act
        var (ok, jti, sub, iat, exp) = service.TryReadAccessToken(accessToken);

        // Assert
        ok.Should().BeTrue();
        jti.Should().NotBeNullOrEmpty();
        sub.Should().Be(user.Id);
        iat.Should().NotBeNull();
        exp.Should().NotBeNull();
    }

    [Fact]
    public void TryReadAccessToken_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var (ok, jti, sub, iat, exp) = service.TryReadAccessToken("invalid.token.here");

        // Assert
        ok.Should().BeFalse();
        jti.Should().BeNull();
        sub.Should().BeNull();
        iat.Should().BeNull();
        exp.Should().BeNull();
    }

    [Fact]
    public void TryReadAccessToken_EmptyToken_ReturnsFalse()
    {
        // Arrange
        var context = InMemoryDbHelper.CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var (ok, _, _, _, _) = service.TryReadAccessToken("");

        // Assert
        ok.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private TokenService CreateService(DigiTekShopIdentityDbContext context)
    {
        return new TokenService(
            _userManager,
            context,
            _timeProvider,
            _jwtOptions,
            _blacklist,
            _currentClient,
            _logger);
    }

    #endregion
}

