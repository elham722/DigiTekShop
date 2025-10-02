namespace DigiTekShop.Contracts.DTOs.EmailSender;

public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public bool UseDefaultCredentials { get; set; } = false;

    public int TimeoutMs { get; set; } = 10000;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public string? ReplyToEmail { get; set; }

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryDelayMs { get; set; } = 1000;
}