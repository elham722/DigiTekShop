using DigiTekShop.Contracts.Interfaces.ExternalServices.PhoneSender;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using DigiTekShop.ExternalServices.Sms.Models;

namespace DigiTekShop.ExternalServices.Sms;

public class KavenegarSmsSender : IPhoneSender
{
    private readonly KavenegarSettings _settings;
    private readonly ILogger<KavenegarSmsSender> _logger;
    private readonly HttpClient _httpClient;

    public PhoneSenderSettings Settings { get; }

    public KavenegarSmsSender(
        IOptions<KavenegarSettings> settings,
        ILogger<KavenegarSmsSender> logger,
        HttpClient httpClient)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        Settings = new PhoneSenderSettings
        {
            ProviderName = "Kavenegar",
            MaxRetryAttempts = 3,
            RetryDelayMs = 1000,
            TimeoutMs = 10000,
            LogMessageContent = false,
            ProviderSettings = new Dictionary<string, string>
            {
                { "ApiKey", _settings.ApiKey },
                { "LineNumber", _settings.LineNumber }
            }
        };
    }

    public async Task<Result> SendCodeAsync(string phoneNumber, string code, string? templateName = null)
    {
        try
        {
            ValidateSettings();
            ValidateInput(phoneNumber, code);

            var message = CreateVerificationMessage(code, templateName);

            var request = new KavenegarRequest
            {
                Receptor = phoneNumber,
                Message = message,
                Sender = _settings.LineNumber
            };

            var response = await SendSmsWithRetryAsync(request);

            if (response.IsSuccess)
            {
                _logger.LogInformation("✅ SMS sent successfully to {PhoneNumber}", phoneNumber);
                return Result.Success();
            }

            _logger.LogError("❌ Failed to send SMS to {PhoneNumber}. Error: {Error}", phoneNumber, response.ErrorMessage);
            return Result.Failure($"Failed to send SMS: {response.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending SMS to {PhoneNumber}", phoneNumber);
            return Result.Failure($"Error sending SMS: {ex.Message}");
        }
    }

    public async Task<Result> SendBulkCodeAsync(PhoneCodeRequest[] requests)
    {
        try
        {
            Guard.AgainstNullOrEmptyCollection(requests, nameof(requests));

            var results = new List<(string PhoneNumber, Result Result)>();

            foreach (var request in requests)
            {
                var result = await SendCodeAsync(request.PhoneNumber, request.Code, request.TemplateName);
                results.Add((request.PhoneNumber, result));
            }

            var failures = results.Where(r => r.Result.IsFailure).ToList();
            if (failures.Any())
            {
                var failedNumbers = string.Join(", ", failures.Select(f => f.PhoneNumber));
                return Result.Failure($"Failed to send SMS to: {failedNumbers}");
            }

            _logger.LogInformation("✅ Bulk SMS sent successfully to {Count} recipients", requests.Length);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending bulk SMS");
            return Result.Failure($"Error sending bulk SMS: {ex.Message}");
        }
    }

    public async Task<Result> TestConnectionAsync()
    {
        try
        {
            ValidateSettings();

            var url = $"{_settings.BaseUrl}/account/info.json";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Kavenegar connection test successful");
                return Result.Success();
            }

            _logger.LogError("❌ Kavenegar connection test failed. Status: {StatusCode}", response.StatusCode);
            return Result.Failure($"Connection failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Kavenegar connection test error");
            return Result.Failure($"Connection test error: {ex.Message}");
        }
    }

    public async Task<Result<double>> GetCreditAsync()
    {
        try
        {
            ValidateSettings();

            var url = $"{_settings.BaseUrl}/account/info.json";
            var response = await _httpClient.GetStringAsync(url);

            var info = JsonSerializer.Deserialize<KavenegarAccountInfo>(response);

            _logger.LogInformation("✅ Kavenegar credit retrieved: {Credit}", info?.Credit ?? 0);
            return Result<double>.Success(info?.Credit ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting Kavenegar credit");
            return Result<double>.Failure($"Error getting credit: {ex.Message}");
        }
    }

    #region Private Helpers

    private void ValidateSettings()
    {
        Guard.AgainstNullOrEmpty(_settings.ApiKey, nameof(_settings.ApiKey));
        Guard.AgainstNullOrEmpty(_settings.BaseUrl, nameof(_settings.BaseUrl));
        Guard.AgainstNullOrEmpty(_settings.LineNumber, nameof(_settings.LineNumber));
    }

    private static void ValidateInput(string phoneNumber, string code)
    {
        Guard.AgainstNullOrEmpty(phoneNumber, nameof(phoneNumber));
        Guard.AgainstNullOrEmpty(code, nameof(code));
        Guard.AgainstInvalidPhoneNumber(phoneNumber);
    }

    private static string CreateVerificationMessage(string code, string? templateName)
    {
        return !string.IsNullOrWhiteSpace(templateName)
            ? templateName.Replace("{code}", code)
            : $"کد تأیید شما: {code}";
    }

    private async Task<KavenegarResponse> SendSmsWithRetryAsync(KavenegarRequest request)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts < Settings.MaxRetryAttempts)
        {
            try
            {
                var url = $"{_settings.BaseUrl}/sms/send.json";
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("receptor", request.Receptor),
                    new KeyValuePair<string, string>("sender", request.Sender),
                    new KeyValuePair<string, string>("message", request.Message)
                });

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");

                var response = await _httpClient.PostAsync(url, formData);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var kavenegarResponse = JsonSerializer.Deserialize<KavenegarApiResponse>(responseContent);

                    return new KavenegarResponse
                    {
                        IsSuccess = kavenegarResponse?.Success ?? false,
                        ErrorMessage = kavenegarResponse?.Message ?? "Unknown error"
                    };
                }

                throw new HttpRequestException($"HTTP {response.StatusCode}: {responseContent}");
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempts++;

                if (attempts < Settings.MaxRetryAttempts)
                {
                    _logger.LogWarning(ex,
                        "SMS send attempt {Attempt} failed, retrying in {Delay}ms",
                        attempts, Settings.RetryDelayMs);

                    await Task.Delay(Settings.RetryDelayMs);
                }
            }
        }

        return new KavenegarResponse
        {
            IsSuccess = false,
            ErrorMessage = lastException?.Message ?? "Unknown error"
        };
    }

    #endregion
}
