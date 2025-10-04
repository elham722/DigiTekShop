namespace DigiTekShop.Contracts.DTOs.ResetPassword;

/// <summary>
/// DTO for forgot password request
/// </summary>
public sealed record ForgotPasswordDto
{
    /// <summary>
    /// User's email address for password reset
    /// </summary>
    public string Email { get; init; } = string.Empty;
}