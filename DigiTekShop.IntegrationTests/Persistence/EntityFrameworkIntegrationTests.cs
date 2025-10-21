using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using Xunit;

namespace DigiTekShop.IntegrationTests.Persistence;

[Collection("Integration")]
public sealed class EntityFrameworkIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly DigiTekShopIdentityDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public EntityFrameworkIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DigiTekShopIdentityDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<DigiTekShopIdentityDbContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}"));
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<DigiTekShopIdentityDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    }

    private readonly HttpClient _client;

    public async Task InitializeAsync()
    {
        // Ensure database is created
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        _client.Dispose();
    }

    #region Database Connection Tests

    [Fact]
    public async Task Database_CanConnect_ShouldReturnTrue()
    {
        // Act
        var canConnect = await _dbContext.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task Database_ExecuteRawQuery_ShouldSucceed()
    {
        // Act
        var result = await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region Migration Tests

    [Fact]
    public async Task Database_GetPendingMigrations_ShouldReturnEmpty()
    {
        // Act
        var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();

        // Assert
        pendingMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task Database_GetAppliedMigrations_ShouldNotBeEmpty()
    {
        // Act
        var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync();

        // Assert
        appliedMigrations.Should().NotBeEmpty();
    }

    #endregion

    #region User Management Tests

    [Fact]
    public async Task UserManager_CreateUser_ShouldSucceed()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        // Act
        var result = await _userManager.CreateAsync(user, "TestPassword123!");

        // Assert
        result.Succeeded.Should().BeTrue();
        
        var createdUser = await _userManager.FindByIdAsync(user.Id.ToString());
        createdUser.Should().NotBeNull();
        createdUser!.UserName.Should().Be("testuser");
        createdUser.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task UserManager_CreateUserWithDuplicateEmail_ShouldFail()
    {
        // Arrange
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            UserName = "user1",
            Email = "duplicate@example.com",
            EmailConfirmed = true
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            UserName = "user2",
            Email = "duplicate@example.com",
            EmailConfirmed = true
        };

        // Act
        await _userManager.CreateAsync(user1, "TestPassword123!");
        var result = await _userManager.CreateAsync(user2, "TestPassword123!");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "DuplicateEmail");
    }

    [Fact]
    public async Task UserManager_CheckPassword_ShouldSucceed()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(user, "TestPassword123!");

        // Act
        var isValid = await _userManager.CheckPasswordAsync(user, "TestPassword123!");

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task UserManager_CheckPasswordWithWrongPassword_ShouldFail()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(user, "TestPassword123!");

        // Act
        var isValid = await _userManager.CheckPasswordAsync(user, "WrongPassword123!");

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Role Management Tests

    [Fact]
    public async Task RoleManager_CreateRole_ShouldSucceed()
    {
        // Arrange
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "TestRole",
            NormalizedName = "TESTROLE"
        };

        // Act
        var result = await _roleManager.CreateAsync(role);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        var createdRole = await _roleManager.FindByIdAsync(role.Id.ToString());
        createdRole.Should().NotBeNull();
        createdRole!.Name.Should().Be("TestRole");
    }

    [Fact]
    public async Task RoleManager_AddUserToRole_ShouldSucceed()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "TestRole",
            NormalizedName = "TESTROLE"
        };

        await _userManager.CreateAsync(user, "TestPassword123!");
        await _roleManager.CreateAsync(role);

        // Act
        var result = await _userManager.AddToRoleAsync(user, "TestRole");

        // Assert
        result.Succeeded.Should().BeTrue();
        
        var isInRole = await _userManager.IsInRoleAsync(user, "TestRole");
        isInRole.Should().BeTrue();
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_CreateAndRetrieve_ShouldSucceed()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(user, "TestPassword123!");

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            DeviceId = "test-device",
            UserAgent = "test-agent",
            IpAddress = "127.0.0.1"
        };

        // Act
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        var retrievedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == refreshToken.Id);

        // Assert
        retrievedToken.Should().NotBeNull();
        retrievedToken!.UserId.Should().Be(user.Id);
        retrievedToken.TokenHash.Should().Be("hashed-token");
        retrievedToken.DeviceId.Should().Be("test-device");
    }

    [Fact]
    public async Task RefreshToken_RevokeToken_ShouldSucceed()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(user, "TestPassword123!");

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            DeviceId = "test-device",
            UserAgent = "test-agent",
            IpAddress = "127.0.0.1"
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        // Act
        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var retrievedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == refreshToken.Id);

        // Assert
        retrievedToken.Should().NotBeNull();
        retrievedToken!.RevokedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshToken_RevokeAllUserTokens_ShouldSucceed()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(user, "TestPassword123!");

        var refreshTokens = new[]
        {
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "hashed-token-1",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow,
                DeviceId = "device-1",
                UserAgent = "agent-1",
                IpAddress = "127.0.0.1"
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "hashed-token-2",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow,
                DeviceId = "device-2",
                UserAgent = "agent-2",
                IpAddress = "127.0.0.2"
            }
        };

        _dbContext.RefreshTokens.AddRange(refreshTokens);
        await _dbContext.SaveChangesAsync();

        // Act
        var revokedAt = DateTime.UtcNow;
        var tokensToRevoke = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var token in tokensToRevoke)
        {
            token.RevokedAtUtc = revokedAt;
        }

        await _dbContext.SaveChangesAsync();

        // Assert
        var revokedTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync();

        revokedTokens.Should().HaveCount(2);
        revokedTokens.Should().AllSatisfy(rt => rt.RevokedAtUtc.Should().NotBeNull());
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task Database_ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();
        var users = new List<User>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = $"user{i}",
                Email = $"user{i}@example.com",
                EmailConfirmed = true
            };

            users.Add(user);
            tasks.Add(_userManager.CreateAsync(user, "TestPassword123!"));
        }

        await Task.WhenAll(tasks);

        // Assert
        var createdUsers = await _dbContext.Users.ToListAsync();
        createdUsers.Should().HaveCount(10);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task Database_Transaction_ShouldRollbackOnFailure()
    {
        // Arrange
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                Email = "test@example.com",
                EmailConfirmed = true
            };

            await _userManager.CreateAsync(user, "TestPassword123!");

            // Simulate an error
            throw new InvalidOperationException("Simulated error");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
        }

        // Assert
        var userExists = await _userManager.FindByNameAsync("testuser");
        userExists.Should().BeNull();
    }

    #endregion
}
