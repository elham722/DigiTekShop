using DigiTekShop.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.ExternalServices.Email.Options;
using DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;

namespace DigiTekShop.ExternalServices.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpSettings> settings,
        ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        string? plainTextContent = null)
    {
        try
        {
            ValidateSettings();
            ValidateInput(toEmail, subject);

            using var smtpClient = CreateSmtpClient();
            using var message = CreateMailMessage(toEmail, subject, htmlContent, plainTextContent);

            await SendWithRetryAsync(smtpClient, message);

            _logger.LogInformation("✅ Email sent to {ToEmail} with subject: {Subject}", toEmail, subject);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send email to {ToEmail}", toEmail);
            return Result.Failure($"Failed to send email: {ex.Message}");
        }
    }

    public async Task<Result> SendBulkEmailAsync(
        string[] toEmails,
        string subject,
        string htmlContent,
        string? plainTextContent = null)
    {
        try
        {
            ValidateSettings();
            Guard.AgainstNullOrEmptyCollection(toEmails, nameof(toEmails));

            var results = new List<(string Email, Result Result)>();
            using var smtpClient = CreateSmtpClient();

            foreach (var toEmail in toEmails)
            {
                try
                {
                    ValidateInput(toEmail, subject);

                    using var message = CreateMailMessage(toEmail, subject, htmlContent, plainTextContent);
                    await SendWithRetryAsync(smtpClient, message);

                    results.Add((toEmail, Result.Success()));
                    _logger.LogDebug("Bulk email sent to {ToEmail}", toEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bulk email failed for {ToEmail}", toEmail);
                    results.Add((toEmail, Result.Failure(ex.Message)));
                }
            }

            var failures = results.Where(r => r.Result.IsFailure).ToList();
            if (failures.Any())
            {
                var failedEmails = string.Join(", ", failures.Select(f => f.Email));
                return Result.Failure($"Failed to send emails to: {failedEmails}");
            }

            _logger.LogInformation("✅ Bulk email sent to {Count} recipients", toEmails.Length);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Bulk email operation failed");
            return Result.Failure($"Bulk email failed: {ex.Message}");
        }
    }

    public async Task<Result> TestConnectionAsync()
    {
        try
        {
            ValidateSettings();

            using var smtpClient = CreateSmtpClient();
            using var testMessage = new MailMessage(_settings.FromEmail, _settings.FromEmail)
            {
                Subject = "SMTP Test",
                Body = "SMTP connection test message."
            };

            await smtpClient.SendMailAsync(testMessage);

            _logger.LogInformation("✅ SMTP connection test successful");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SMTP connection test failed");
            return Result.Failure($"SMTP connection test failed: {ex.Message}");
        }
    }

    #region Private Helpers

    private void ValidateSettings()
    {
        Guard.AgainstNullOrEmpty(_settings.Host, nameof(_settings.Host));
        Guard.AgainstOutOfRange(_settings.Port, 1, 65535, nameof(_settings.Port));
        Guard.AgainstNullOrEmpty(_settings.FromEmail, nameof(_settings.FromEmail));
        Guard.AgainstEmail(_settings.FromEmail, nameof(_settings.FromEmail));

        if (!_settings.UseDefaultCredentials)
            Guard.AgainstNullOrEmpty(_settings.Username, nameof(_settings.Username));
    }

    private static void ValidateInput(string toEmail, string subject)
    {
        Guard.AgainstNullOrEmpty(toEmail, nameof(toEmail));
        Guard.AgainstEmail(toEmail, nameof(toEmail));
        Guard.AgainstNullOrEmpty(subject, nameof(subject));
    }

    private SmtpClient CreateSmtpClient()
    {
        var smtpClient = new SmtpClient
        {
            Host = _settings.Host,
            Port = _settings.Port,
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = _settings.UseDefaultCredentials,
            Timeout = _settings.TimeoutMs
        };


        if (!_settings.UseDefaultCredentials)
            smtpClient.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

        return smtpClient;
    }

    private MailMessage CreateMailMessage(string toEmail, string subject, string htmlContent, string? plainTextContent)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            SubjectEncoding = System.Text.Encoding.UTF8,
            BodyEncoding = System.Text.Encoding.UTF8,
            // اگر فقط html داری:
            IsBodyHtml = !string.IsNullOrWhiteSpace(htmlContent),
            Body = !string.IsNullOrWhiteSpace(htmlContent) ? htmlContent : plainTextContent ?? string.Empty
        };

        message.To.Add(toEmail);

        if (!string.IsNullOrWhiteSpace(_settings.ReplyToEmail))
            message.ReplyToList.Add(_settings.ReplyToEmail);

        // اگر هر دو قالب را داری، استاندارد: multipart/alternative
        if (!string.IsNullOrWhiteSpace(htmlContent) && !string.IsNullOrWhiteSpace(plainTextContent))
        {
            message.AlternateViews.Clear();
            var plain = AlternateView.CreateAlternateViewFromString(plainTextContent, null, "text/plain");
            var html = AlternateView.CreateAlternateViewFromString(htmlContent, null, "text/html");
            message.AlternateViews.Add(plain);
            message.AlternateViews.Add(html);
            message.Body = plainTextContent; // بدنه پیش‌فرض plain؛ کلاینت‌ها html را ترجیح می‌دهند
            message.IsBodyHtml = false;
        }

        return message;
    }

    private async Task SendWithRetryAsync(SmtpClient smtpClient, MailMessage message)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts < _settings.MaxRetryAttempts)
        {
            try
            {
                await smtpClient.SendMailAsync(message);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempts++;

                if (attempts < _settings.MaxRetryAttempts)
                {
                    _logger.LogWarning(ex,
                        "SMTP send attempt {Attempt} failed, retrying in {Delay}ms",
                        attempts, _settings.RetryDelayMs);

                    await Task.Delay(_settings.RetryDelayMs);
                }
            }
        }

        throw lastException ?? new InvalidOperationException("Unknown error occurred during SMTP send.");
    }

    #endregion
}
