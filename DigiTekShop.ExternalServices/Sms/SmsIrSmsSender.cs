using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Results;
using DigiTekShop.SharedKernel.Utilities.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace DigiTekShop.ExternalServices.Sms;

public class SmsIrSmsSender : IPhoneSender
{
    private readonly SmsIrSettings _opt;
    private readonly ILogger<SmsIrSmsSender> _log;
    private readonly HttpClient _http;

    public SmsIrSmsSender(
        IOptions<SmsIrSettings> opt,
        ILogger<SmsIrSmsSender> log,
        IHttpClientFactory httpFactory)
    {
        _opt = opt.Value;
        _log = log;
        _http = httpFactory.CreateClient(nameof(SmsIrSmsSender));
    }

    public async Task<Result> SendCodeAsync(string phoneNumber, string code, string? templateName = null, CancellationToken ct = default)
    {
        // 1) TemplateId نهایی را تعیین کن
        int templateId = _opt.TemplateId;
        if (!string.IsNullOrWhiteSpace(templateName) && int.TryParse(templateName, out var parsed))
            templateId = parsed;

        // 2) شماره را برای SMS.ir normalize کن (E.164 → ملی)
        var mobile = NormalizeForSmsIr(phoneNumber);
        if (string.IsNullOrWhiteSpace(mobile))
            return Result.Failure("invalid mobile for Sms.ir");

        // 3) بدنه طبق سند Sandbox
        var payload = new
        {
            mobile,
            templateId,
            parameters = new[]
            {
                new { name = _opt.TemplateParamName, value = code }
            }
        };

        var path = _opt.UltraFastPath?.Trim('/') ?? "send/verify";
        var json = JsonSerializer.Serialize(payload);

        if (_opt.LogRequestBody)
        {
            // Mask OTP code for security
            var maskedPayload = new
            {
                mobile,
                templateId,
                parameters = new[]
                {
                    new { name = _opt.TemplateParamName, value = "****" }
                }
            };
            var maskedJson = JsonSerializer.Serialize(maskedPayload);
            _log.LogInformation("[SmsIr] POST {Path} body={Body}", path, maskedJson);
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage resp;
        string body;
        try
        {
            resp = await _http.SendAsync(req, ct);
            body = await resp.Content.ReadAsStringAsync(ct);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !ct.IsCancellationRequested)
        {
            _log.LogError(ex, "[SmsIr] Request timeout after {Timeout}s for phone={Phone}", _opt.TimeoutSeconds, SensitiveDataMasker.MaskPhone(mobile));
            return Result.Failure($"SMS.ir request timeout after {_opt.TimeoutSeconds} seconds");
        }
        catch (HttpRequestException ex)
        {
            _log.LogError(ex, "[SmsIr] HTTP request failed for phone={Phone}", SensitiveDataMasker.MaskPhone(mobile));
            return Result.Failure($"SMS.ir request failed: {ex.Message}");
        }

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogError("[SmsIr] HTTP {Code}: {Body}", (int)resp.StatusCode, body);
            return Result.Failure($"HTTP {(int)resp.StatusCode}: {BodyPreview(body)}");
        }

        // 4) status==1 => موفق
        try
        {
            using var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetInt32() : 0;
            if (status == 1) return Result.Success();

            var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "unknown";
            _log.LogError("[SmsIr] status={Status}, msg={Msg}", status, msg);
            return Result.Failure($"SMS.ir error: {msg ?? "status != 1"} (code={status})");
        }
        catch
        {
            // اگر بدنه text/plain بود ولی 200 گرفتیم، موفق در نظر بگیر
            return Result.Success();
        }
    }

    private static string BodyPreview(string? body)
        => string.IsNullOrEmpty(body) ? "(empty)" : (body.Length <= 300 ? body : body.Substring(0, 300) + "...");

    /// <summary>
    /// +98912xxxxxxx → 0912xxxxxxx
    /// 98912xxxxxxx  → 0912xxxxxxx
    /// 912xxxxxxx    → 0912xxxxxxx
    /// 0912xxxxxxx   → همان
    /// غیر از این‌ها → null
    /// </summary>
    private static string? NormalizeForSmsIr(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var p = input.Trim();

        if (p.StartsWith("+98")) p = "0" + p.Substring(3);
        else if (p.StartsWith("98")) p = "0" + p.Substring(2);
        else if (p.Length == 10 && p.StartsWith("9")) p = "0" + p;
        // اگر 11 رقم و با 0 شروع می‌شود، اوکی است

        // چک ساده: 11 رقم، با 0 شروع، همه رقم
        if (p.Length == 11 && p.StartsWith("0") && p.All(char.IsDigit))
            return p;

        return null;
    }
}
