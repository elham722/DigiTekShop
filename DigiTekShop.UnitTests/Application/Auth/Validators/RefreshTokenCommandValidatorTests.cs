using DigiTekShop.Application.Auth.Tokens.Command;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using FluentValidation.TestHelper;
using Xunit;

namespace DigiTekShop.UnitTests.Application.Auth.Validators;

public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator;

    public RefreshTokenCommandValidatorTests()
    {
        _validator = new RefreshTokenCommandValidator();
    }

    #region Positive Test Cases

    [Theory]
    [InlineData("valid-refresh-token-123")]
    [InlineData("another-valid-token")]
    [InlineData("token-with-numbers123")]
    [InlineData("very-long-refresh-token-that-is-still-valid")]
    public void Validate_WithValidRefreshToken_ShouldPass(string refreshToken)
    {
        // Arrange
        var command = new RefreshTokenCommand(new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.RefreshToken);
    }

    #endregion

    #region Negative Test Cases

    [Fact]
    public void Validate_WithNullRefreshToken_ShouldFail()
    {
        // Arrange
        var command = new RefreshTokenCommand(new RefreshTokenRequest
        {
            RefreshToken = null
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.RefreshToken)
            .WithErrorMessage("refresh_token الزامی است.");
    }

    [Fact]
    public void Validate_WithEmptyRefreshToken_ShouldFail()
    {
        // Arrange
        var command = new RefreshTokenCommand(new RefreshTokenRequest
        {
            RefreshToken = ""
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.RefreshToken)
            .WithErrorMessage("refresh_token الزامی است.");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyRefreshToken_ShouldFail()
    {
        // Arrange
        var command = new RefreshTokenCommand(new RefreshTokenRequest
        {
            RefreshToken = "   "
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.RefreshToken)
            .WithErrorMessage("refresh_token الزامی است.");
    }

    [Fact]
    public void Validate_WithTooLongRefreshToken_ShouldFail()
    {
        // Arrange
        var longToken = new string('a', 300); // Exceeds 256 character limit
        var command = new RefreshTokenCommand(new RefreshTokenRequest
        {
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
        var command = new RefreshTokenCommand(null!);

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
        var command = new RefreshTokenCommand(new RefreshTokenRequest
        {
            RefreshToken = shortToken
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.RefreshToken);
    }

    #endregion
}
