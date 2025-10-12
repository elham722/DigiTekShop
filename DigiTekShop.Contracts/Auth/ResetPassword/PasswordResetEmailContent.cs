namespace DigiTekShop.Contracts.Auth.ResetPassword;

public sealed record PasswordResetEmailContent(
    string Subject,
    string HtmlContent,
    string PlainTextContent
);