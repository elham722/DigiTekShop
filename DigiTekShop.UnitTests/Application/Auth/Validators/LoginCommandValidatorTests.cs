using DigiTekShop.Application.Auth.Login.Command;
using DigiTekShop.Contracts.DTOs.Auth.Login;
using FluentValidation.TestHelper;
using Xunit;

namespace DigiTekShop.UnitTests.Application.Auth.Validators;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator();
    }

    #region Positive Test Cases

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test@domain.co.uk")]
    [InlineData("admin@company.org")]
    [InlineData("user123@test-domain.com")]
    public void Validate_WithValidEmail_ShouldPass(string email)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = email,
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.Login);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("admin123")]
    [InlineData("test_user")]
    [InlineData("a")]
    [InlineData("verylongusernamethatexceeds64characterslimitandshouldstillbevalid")]
    public void Validate_WithValidUsername_ShouldPass(string username)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = username,
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.Login);
    }

    [Theory]
    [InlineData("ValidPassword123!")]
    [InlineData("AnotherValidPass456@")]
    [InlineData("ShortPass1!")]
    [InlineData("VeryLongPasswordThatExceedsNormalLengthButIsStillValid123!@#")]
    public void Validate_WithValidPassword_ShouldPass(string password)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = password,
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.Password);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("000000")]
    [InlineData("999999")]
    [InlineData("1234")]
    public void Validate_WithValidTotpCode_ShouldPass(string totpCode)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = "ValidPassword123!",
            TotpCode = totpCode,
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.TotpCode);
    }

    [Theory]
    [InlineData("valid-captcha-token")]
    [InlineData("another-valid-token")]
    [InlineData("token-with-numbers123")]
    [InlineData("very-long-captcha-token-that-exceeds-normal-length-but-is-still-valid")]
    public void Validate_WithValidCaptchaToken_ShouldPass(string captchaToken)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = captchaToken
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.CaptchaToken);
    }

    #endregion

    #region Negative Test Cases - Login Field

    [Fact]
    public void Validate_WithNullLogin_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = null,
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Login)
            .WithErrorMessage("login الزامی است.");
    }

    [Fact]
    public void Validate_WithEmptyLogin_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "",
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Login)
            .WithErrorMessage("login الزامی است.");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyLogin_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "   ",
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Login)
            .WithErrorMessage("login الزامی است.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user.example.com")]
    [InlineData("user@example")]
    public void Validate_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = invalidEmail,
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Login)
            .WithErrorMessage("login باید ایمیل معتبر یا نام‌کاربری 3 تا 64 کاراکتری باشد.");
    }

    [Theory]
    [InlineData("ab")] // Too short username
    [InlineData("a")] // Too short username
    public void Validate_WithTooShortUsername_ShouldFail(string shortUsername)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = shortUsername,
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Login)
            .WithErrorMessage("login باید ایمیل معتبر یا نام‌کاربری 3 تا 64 کاراکتری باشد.");
    }

    [Fact]
    public void Validate_WithTooLongLogin_ShouldFail()
    {
        // Arrange
        var longLogin = new string('a', 300); // Exceeds 256 character limit
        var command = new LoginCommand(new LoginRequest
        {
            Login = longLogin,
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Login);
    }

    #endregion

    #region Negative Test Cases - Password Field

    [Fact]
    public void Validate_WithNullPassword_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = null,
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Password)
            .WithErrorMessage("password الزامی است.");
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = "",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Password)
            .WithErrorMessage("password الزامی است.");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyPassword_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = "   ",
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Password)
            .WithErrorMessage("password الزامی است.");
    }

    [Fact]
    public void Validate_WithTooLongPassword_ShouldFail()
    {
        // Arrange
        var longPassword = new string('a', 300); // Exceeds 256 character limit
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = longPassword,
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.Password);
    }

    #endregion

    #region Negative Test Cases - Optional Fields

    [Fact]
    public void Validate_WithTooLongTotpCode_ShouldFail()
    {
        // Arrange
        var longTotpCode = new string('1', 20); // Exceeds 16 character limit
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = "ValidPassword123!",
            TotpCode = longTotpCode,
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.TotpCode);
    }

    [Fact]
    public void Validate_WithTooLongCaptchaToken_ShouldFail()
    {
        // Arrange
        var longCaptchaToken = new string('a', 3000); // Exceeds 2048 character limit
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = "ValidPassword123!",
            TotpCode = "123456",
            CaptchaToken = longCaptchaToken
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto.CaptchaToken);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithNullDto_ShouldFail()
    {
        // Arrange
        var command = new LoginCommand(null!);

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Dto);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456")]
    [InlineData("abcdef")]
    [InlineData("12345a")]
    public void Validate_WithValidTotpCodeFormats_ShouldPass(string totpCode)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = "ValidPassword123!",
            TotpCode = totpCode,
            CaptchaToken = "valid-captcha-token"
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.TotpCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullOptionalFields_ShouldPass(string? optionalValue)
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = "user@example.com",
            Password = "ValidPassword123!",
            TotpCode = optionalValue,
            CaptchaToken = optionalValue
        });

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.TotpCode);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.CaptchaToken);
    }

    #endregion

    #region Cascade Mode Tests

    [Fact]
    public void Validate_WithMultipleErrors_ShouldStopAtFirstError()
    {
        // Arrange
        var command = new LoginCommand(new LoginRequest
        {
            Login = null, // First error
            Password = null, // Should not be validated due to cascade mode
            TotpCode = "123456",
            CaptchaToken = "valid-captcha-token"
        });

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.Should().HaveCount(1); // Only first error should be reported
        result.ShouldHaveValidationErrorFor(x => x.Dto.Login);
        result.ShouldNotHaveValidationErrorFor(x => x.Dto.Password); // Should not be validated
    }

    #endregion
}
