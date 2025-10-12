namespace DigiTekShop.Contracts.Auth.EmailConfirmation
{
    public record EmailConfirmationContent(string Subject, string HtmlContent, string PlainTextContent);
}
