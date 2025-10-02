namespace DigiTekShop.Contracts.DTOs.Auth;

/// <summary>
/// DTO for password reset request
/// </summary>
public sealed record ResetPasswordDto
{
    /// <summary>
    /// User ID who is resetting password
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Base64 encoded password reset token
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// New password chosen by user
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;
}