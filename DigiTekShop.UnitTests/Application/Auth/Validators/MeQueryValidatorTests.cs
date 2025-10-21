using DigiTekShop.Application.Auth.Me.Query;
using FluentValidation.TestHelper;
using Xunit;

namespace DigiTekShop.UnitTests.Application.Auth.Validators;

public sealed class MeQueryValidatorTests
{
    private readonly MeQueryValidator _validator;

    public MeQueryValidatorTests()
    {
        _validator = new MeQueryValidator();
    }

    #region Positive Test Cases

    [Fact]
    public void Validate_WithValidQuery_ShouldPass()
    {
        // Arrange
        var query = new MeQuery();

        // Act & Assert
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithNullQuery_ShouldPass()
    {
        // Arrange
        MeQuery? query = null;

        // Act & Assert
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Note

    /*
     * The MeQuery validator is intentionally simple as it doesn't have any input parameters to validate.
     * The actual authorization and user context validation happens at the controller/authentication level.
     * This test ensures the validator structure is correct and can be extended if needed.
     */

    #endregion
}
