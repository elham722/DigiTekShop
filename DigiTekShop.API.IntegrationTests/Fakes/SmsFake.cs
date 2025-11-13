using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;
using DigiTekShop.SharedKernel.Results;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DigiTekShop.API.IntegrationTests.Fakes;

/// <summary>
/// فیک برای IPhoneSender - برای استفاده در تست‌های Integration
/// ذخیره‌سازی پیام‌های ارسالی و استخراج OTP از آنها
/// </summary>
public sealed class SmsFake : IPhoneSender
{
    private readonly ConcurrentBag<SentMessage> _outbox = new();
    
    /// <summary>
    /// لیست پیام‌های ارسال شده
    /// </summary>
    public IReadOnlyList<SentMessage> Sent => _outbox.ToList();

    /// <summary>
    /// فلگ برای شبیه‌سازی خطا در ارسال
    /// </summary>
    public bool SimulateFailure { get; set; }

    public Task<Result> SendCodeAsync(
        string phoneNumber, 
        string code, 
        string? templateName = null, 
        CancellationToken ct = default)
    {
        if (SimulateFailure)
        {
            return Task.FromResult(Result.Failure("SMS_SEND_FAILED", "Simulated SMS send failure"));
        }

        _outbox.Add(new SentMessage(phoneNumber, code, templateName, DateTime.UtcNow));
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// استخراج آخرین OTP ارسال شده به یک شماره خاص
    /// </summary>
    public string? TryExtractOtp(string phoneNumber)
    {
        var lastMsg = _outbox
            .Where(m => NormalizePhone(m.Phone) == NormalizePhone(phoneNumber))
            .OrderByDescending(m => m.SentAtUtc)
            .FirstOrDefault();

        return lastMsg?.Code;
    }

    /// <summary>
    /// پاک کردن تمام پیام‌های ذخیره شده
    /// </summary>
    public void Clear()
    {
        _outbox.Clear();
    }

    /// <summary>
    /// تعداد پیام‌های ارسال شده به یک شماره خاص
    /// </summary>
    public int GetSentCount(string phoneNumber)
    {
        return _outbox.Count(m => NormalizePhone(m.Phone) == NormalizePhone(phoneNumber));
    }

    private static string NormalizePhone(string phone)
    {
        // حذف کاراکترهای غیرعددی
        return Regex.Replace(phone ?? "", @"[^\d+]", "");
    }

    public sealed record SentMessage(
        string Phone, 
        string Code, 
        string? TemplateName, 
        DateTime SentAtUtc);
}

