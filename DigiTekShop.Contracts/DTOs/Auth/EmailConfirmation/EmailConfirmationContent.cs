namespace DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation
{
    public record EmailConfirmationContent(string Subject, string HtmlContent, string PlainTextContent);
}
