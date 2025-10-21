using DigiTekShop.Application.Auth.Logout.Command;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using FluentValidation.TestHelper;
using Xunit;

namespace DigiTekShop.UnitTests.Application.Auth.Validators;

public sealed class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator;

    public LogoutCommandValidatorTests()
    {
        _validator = new LogoutCommandValidator();
    }

    #region Positive Test Cases

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest
        {
            UserId = Guid.NewGuid(),
            RefreshToken = "valid-refresh-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.UserId);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.RefreshToken);
    }

    [Fact]
    public void Validate_WithNullRefreshToken_ShouldPass()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest
        {
            UserId = Guid.NewGuid(),
            RefreshToken = null
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.UserId);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.RefreshToken);
    }

    [Fact]
    public void Validate_WithEmptyRefreshToken_ShouldPass()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest
        {
            UserId = Guid.NewGuid(),
            RefreshToken = ""
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.UserId);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.RefreshToken);
    }

    [Theory]
    [InlineData("valid-refresh-token")]
    [InlineData("another-valid-token")]
    [InlineData("token-with-numbers123")]
    [InlineData("very-long-refresh-token-that-is-still-valid")]
    public void Validate_WithValidRefreshTokens_ShouldPass(string refreshToken)
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest
        {
            UserId = Guid.NewGuid(),
            RefreshToken = refreshToken
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.RefreshToken);
    }

    #endregion

    #region Negative Test Cases

    [Fact]
    public void Validate_WithEmptyGuid_ShouldFail()
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest
        {
            UserId = Guid.Empty,
            RefreshToken = "valid-refresh-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.UserId)
            .WithErrorMessage("user_id الزامی است.");
    }

    [Fact]
    public void Validate_WithTooLongRefreshToken_ShouldFail()
    {
        // Arrange
        var longToken = new string('a', 300); // Exceeds 256 character limit
        var command = new LogoutCommand(new LogoutRequest
        {
            UserId = Guid.NewGuid(),
            RefreshToken = longToken
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.RefreshToken);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithNullDto_ShouldFail()
    {
        // Arrange
        var command = new LogoutCommand(null!);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    public void Validate_WithShortValidTokens_ShouldPass(string shortToken)
    {
        // Arrange
        var command = new LogoutCommand(new LogoutRequest
        {
            UserId = Guid.NewGuid(),
            RefreshToken = shortToken
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.RefreshToken);
    }

    #endregion
}
