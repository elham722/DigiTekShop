namespace DigiTekShop.Contracts.DTOs.Auth.ResetPassword;

/// <summary>
/// Settings for password reset functionality
/// </summary>
public sealed class PasswordResetSettings
{
    /// <summary>
    /// Base URL for password reset links (frontend URL)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Path for password reset page
    /// </summary>
    public string ResetPasswordPath { get; set; } = "auth/reset-password";

    /// <summary>
    /// Token validity duration in minutes
    /// </summary>
    public int TokenValidityMinutes { get; set; } = 60; // 1 hour

    /// <summary>
    /// Whether password reset is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Allow multiple reset requests per user
    /// </summary>
    public bool AllowMultipleRequests { get; set; } = true;

    /// <summary>
    /// Cooldown period between reset requests in minutes
    /// </summary>
    public int RequestCooldownMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum reset requests per day per user
    /// </summary>
    public int MaxRequestsPerDay { get; set; } = 5;

    /// <summary>
    /// Email template settings for password reset
    /// </summary>
    public PasswordResetEmailTemplate Template { get; set; } = new();
}

/// <summary>
/// Email template settings for password reset emails
/// </summary>
public sealed class PasswordResetEmailTemplate
{
    /// <summary>
    /// Company name for email branding
    /// </summary>
    public string CompanyName { get; set; } = "DigiTekShop";

    /// <summary>
    /// Support email for contact
    /// </summary>
    public string SupportEmail { get; set; } = string.Empty;

    /// <summary>
    /// Logo URL for email branding
    /// </summary>
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Primary color for branding
    /// </summary>
    public string PrimaryColor { get; set; } = "#007bff";

    /// <summary>
    /// Contact page URL
    /// </summary>
    public string ContactUrl { get; set; } = string.Empty;

    /// <summary>
    /// Web application URL
    /// </summary>
    public string WebUrl { get; set; } = string.Empty;
}