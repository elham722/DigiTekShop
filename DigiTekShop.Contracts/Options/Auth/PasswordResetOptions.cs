namespace DigiTekShop.Contracts.Options.Auth;
public sealed class PasswordResetOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ResetPasswordPath { get; init; } = "auth/reset-password";
    public int TokenValidityMinutes { get; init; } = 60;
    public bool IsEnabled { get; init; } = true;
    public bool AllowMultipleRequests { get; init; } = true;
    public int RequestCooldownMinutes { get; init; } = 5;
    public int MaxRequestsPerDay { get; init; } = 5;
    public EmailTemplateOptions Template { get; init; } = new();
}

