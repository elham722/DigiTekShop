using DigiTekShop.Contracts.Interfaces.ExternalServices.PhoneSender;
using DigiTekShop.ExternalServices.Sms.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.ExternalServices.Sms;

/// <summary>
/// Mock SMS sender for development/testing - logs instead of sending actual SMS
/// </summary>
public class MockSmsSender : IPhoneSender
{
    private readonly ILogger<MockSmsSender> _logger;
    private readonly string? _logFilePath;

    public PhoneSenderSettings Settings { get; }

    public MockSmsSender(ILogger<MockSmsSender> logger, string? logFilePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logFilePath = logFilePath;
        
        Settings = new PhoneSenderSettings
        {
            ProviderName = "Mock",
            MaxRetryAttempts = 1,
            RetryDelayMs = 0,
            TimeoutMs = 0,
            LogMessageContent = true,
            ProviderSettings = new Dictionary<string, string>
            {
                { "LogFilePath", _logFilePath ?? "logs/sms-mock.log" }
            }
        };
    }

    public Task<Result> SendCodeAsync(string phoneNumber, string code, string? templateName = null)
    {
        var timestamp = DateTime.UtcNow;
        var message = $"کد تأیید شما: {code} - DigiTekShop";
        var logMessage = $"[MOCK SMS] {timestamp:yyyy-MM-dd HH:mm:ss} | To: {phoneNumber} | Code: {code}";

        // Log to console
        _logger.LogInformation("📱 {LogMessage}", logMessage);

        // Log to file if path provided
        if (!string.IsNullOrWhiteSpace(_logFilePath))
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write SMS log to file: {FilePath}", _logFilePath);
            }
        }

        return Task.FromResult(Result.Success());
    }

    public async Task<Result> SendBulkCodeAsync(PhoneCodeRequest[] requests)
    {
        if (requests == null || requests.Length == 0)
            return Result.Failure("No requests provided");

        foreach (var request in requests)
        {
            await SendCodeAsync(request.PhoneNumber, request.Code, request.TemplateName);
        }

        _logger.LogInformation("📱 [MOCK SMS] Bulk sent to {Count} recipients", requests.Length);
        return Result.Success();
    }

    public Task<Result> TestConnectionAsync()
    {
        _logger.LogInformation("📱 [MOCK SMS] Connection test - always succeeds");
        return Task.FromResult(Result.Success());
    }

    public Task<Result<double>> GetCreditAsync()
    {
        _logger.LogInformation("📱 [MOCK SMS] Credit check - returning mock value: 999999");
        return Task.FromResult(Result<double>.Success(999999.0));
    }
}

