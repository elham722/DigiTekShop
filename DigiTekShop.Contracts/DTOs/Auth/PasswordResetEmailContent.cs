namespace DigiTekShop.Contracts.DTOs.Auth;

/// <summary>
/// Email content for password reset emails
/// </summary>
public sealed record PasswordResetEmailContent(
    string Subject,
    string HtmlContent,
    string PlainTextContent
);