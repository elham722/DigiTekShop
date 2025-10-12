// DigiTekShop.ExternalServices.Sms.KavenegarSmsSender
using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;
using DigiTekShop.ExternalServices.Sms.Models;
using DigiTekShop.ExternalServices.Sms.Options;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;

namespace DigiTekShop.ExternalServices.Sms;

public class KavenegarSmsSender : IPhoneSender
{
    private readonly KavenegarSettings _settings;
    private readonly ILogger<KavenegarSmsSender> _logger;
    private readonly HttpClient _http;

    public PhoneSenderSettings Settings { get; }

    public KavenegarSmsSender(
        IOptions<KavenegarSettings> settings,
        ILogger<KavenegarSmsSender> logger,
        HttpClient httpClient)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        Settings = new PhoneSenderSettings
        {
            ProviderName = "Kavenegar",
            MaxRetryAttempts = 3,
            RetryDelayMs = 1000,
            TimeoutMs = (_settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 10) * 1000,
            LogMessageContent = false,
            ProviderSettings = new Dictionary<string, string>
            {
                { "ApiKey", _settings.ApiKey },
                { "DefaultSender", _settings.DefaultSender }
            }
        };
    }

    public async Task<Result> SendCodeAsync(string phoneNumber, string code, string? templateName = null)
    {
        try
        {
            ValidateSettings();
            Guard.AgainstNullOrEmpty(phoneNumber, nameof(phoneNumber));
            Guard.AgainstInvalidPhoneNumber(phoneNumber);
            Guard.AgainstNullOrEmpty(code, nameof(code));

            // 1) Try OTP Template via GET (if provided)
            if (!string.IsNullOrWhiteSpace(templateName))
            {
                // /v1/{apikey}/verify/lookup.json?receptor=...&token=...&template=...
                var qs = $"verify/lookup.json?receptor={Uri.EscapeDataString(phoneNumber)}&token={Uri.EscapeDataString(code)}&template={Uri.EscapeDataString(templateName)}";
                var resp = await GetWithRetryAsync(qs);
                
                if (resp.IsSuccess) return Result.Success();

                // If template fails (HTTP 426 = not enabled), fallback to plain SMS
                _logger.LogWarning("Kavenegar OTP template failed: {Err}, falling back to plain SMS", resp.ErrorMessage);
            }

            // 2) Fallback: Plain SMS via POST
            var message = $"کد تایید شما: {code}";
            var form = new Dictionary<string, string>
            {
                ["receptor"] = phoneNumber,
                ["sender"] = _settings.DefaultSender ?? string.Empty,
                ["message"] = message
            };
            var resp2 = await PostWithRetryAsync("sms/send.json", new FormUrlEncodedContent(form));
            if (resp2.IsSuccess) return Result.Success();

            _logger.LogError("Kavenegar SMS error: {Err}", resp2.ErrorMessage);
            return Result.Failure(resp2.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {Phone}", phoneNumber);
            return Result.Failure($"Error sending SMS: {ex.Message}");
        }
    }

    public async Task<Result> SendBulkCodeAsync(PhoneCodeRequest[] requests)
    {
        if (requests == null || requests.Length == 0)
            return Result.Failure("No requests provided.");

        var fails = new List<string>();
        foreach (var r in requests)
        {
            var res = await SendCodeAsync(r.PhoneNumber, r.Code, r.TemplateName);
            if (res.IsFailure) fails.Add(r.PhoneNumber);
        }
        return fails.Count == 0 ? Result.Success()
                                : Result.Failure("Failed: " + string.Join(", ", fails));
    }

    public async Task<Result> TestConnectionAsync()
    {
        try
        {
            // ساده‌ترین تست: یک GET سبک به root apikey
            var resp = await _http.GetAsync(""); // BaseAddress ست شده
            return resp.IsSuccessStatusCode ? Result.Success()
                                            : Result.Failure($"HTTP {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kavenegar test failed");
            return Result.Failure(ex.Message);
        }
    }

    public Task<Result<double>> GetCreditAsync()
    {
        // اگر نیاز داری، می‌تونی endpoint معادل credit را پیاده کنی (کاوه‌نگار عمومی نمی‌دهد)
        return Task.FromResult(Result<double>.Failure("Not supported by this sender."));
    }

    // ------------------ Helpers ------------------

    private void ValidateSettings()
    {
        Guard.AgainstNullOrEmpty(_settings.ApiKey, nameof(_settings.ApiKey));
        Guard.AgainstNullOrEmpty(_settings.BaseUrl, nameof(_settings.BaseUrl));
        Guard.AgainstNullOrEmpty(_settings.DefaultSender, nameof(_settings.DefaultSender));
    }

    private async Task<KavenegarResponse> GetWithRetryAsync(string relativeUrl)
    {
        var attempts = 0; Exception? last = null;

        while (attempts < Settings.MaxRetryAttempts)
        {
            try
            {
                var resp = await _http.GetAsync(relativeUrl);
                var txt = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                    return new KavenegarResponse { IsSuccess = true };

                return new KavenegarResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"HTTP {(int)resp.StatusCode} - {txt}"
                };
            }
            catch (Exception ex)
            {
                last = ex; attempts++;
                if (attempts < Settings.MaxRetryAttempts)
                    await Task.Delay(Settings.RetryDelayMs);
            }
        }

        return new KavenegarResponse { IsSuccess = false, ErrorMessage = last?.Message ?? "Unknown error" };
    }

    private async Task<KavenegarResponse> PostWithRetryAsync(string relativeUrl, HttpContent content)
    {
        var attempts = 0; Exception? last = null;

        while (attempts < Settings.MaxRetryAttempts)
        {
            try
            {
                var resp = await _http.PostAsync(relativeUrl, content);
                var txt = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                    return new KavenegarResponse { IsSuccess = true };

                return new KavenegarResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"HTTP {(int)resp.StatusCode} - {txt}"
                };
            }
            catch (Exception ex)
            {
                last = ex; attempts++;
                if (attempts < Settings.MaxRetryAttempts)
                    await Task.Delay(Settings.RetryDelayMs);
            }
        }

        return new KavenegarResponse { IsSuccess = false, ErrorMessage = last?.Message ?? "Unknown error" };
    }

  

}
