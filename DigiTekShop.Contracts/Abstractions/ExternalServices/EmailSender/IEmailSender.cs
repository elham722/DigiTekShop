namespace DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;

public interface IEmailSender
{
    Task<Result> SendEmailAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null);
    Task<Result> SendBulkEmailAsync(string[] toEmails, string subject, string htmlContent, string? plainTextContent = null);
    Task<Result> TestConnectionAsync();
}