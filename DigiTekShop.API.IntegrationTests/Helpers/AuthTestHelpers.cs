using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace DigiTekShop.API.IntegrationTests.Helpers;

/// <summary>
/// Helper‌های مشترک برای تست‌های Auth
/// </summary>
public static class AuthTestHelpers
{
    /// <summary>
    /// استخراج LoginResponseDto از پاسخ API
    /// </summary>
    public static async Task<LoginResponseDto?> ExtractLoginResponseAsync(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // فرض می‌کنیم پاسخ به صورت ApiResponse<LoginResponseDto> است
        var doc = JsonDocument.Parse(content);
        
        // اگر data property موجود است، از آن استفاده کن
        if (doc.RootElement.TryGetProperty("data", out var dataElement))
        {
            return JsonSerializer.Deserialize<LoginResponseDto>(dataElement.GetRawText(), 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // اگر مستقیم LoginResponseDto است
        return JsonSerializer.Deserialize<LoginResponseDto>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    /// <summary>
    /// Parse کردن JWT token بدون اعتبارسنجی (برای تست)
    /// </summary>
    public static JwtSecurityToken? ParseJwtToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return null;

            return handler.ReadJwtToken(token);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// استخراج Claim خاص از JWT
    /// </summary>
    public static string? GetClaim(this JwtSecurityToken jwt, string claimType)
    {
        return jwt.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    /// <summary>
    /// استخراج UserId از JWT
    /// </summary>
    public static Guid? GetUserId(this JwtSecurityToken jwt)
    {
        var sub = jwt.GetClaim(JwtRegisteredClaimNames.Sub) 
                  ?? jwt.GetClaim(ClaimTypes.NameIdentifier);
        
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    /// <summary>
    /// استخراج JTI (JWT ID) از توکن
    /// </summary>
    public static string? GetJti(this JwtSecurityToken jwt)
    {
        return jwt.GetClaim(JwtRegisteredClaimNames.Jti);
    }

    /// <summary>
    /// چک کردن اینکه JWT منقضی شده یا نه
    /// </summary>
    public static bool IsExpired(this JwtSecurityToken jwt, DateTime? now = null)
    {
        var checkTime = now ?? DateTime.UtcNow;
        return jwt.ValidTo < checkTime;
    }

    /// <summary>
    /// تولید شماره تلفن یونیک برای تست
    /// </summary>
    public static string GenerateUniquePhone()
    {
        // برای تست‌های موازی، شماره‌های یونیک تولید می‌کنیم
        var random = new Random();
        var suffix = random.Next(1000000, 9999999);
        return $"+9891{suffix}";
    }

    /// <summary>
    /// ارسال OTP و استخراج کد از SmsFake
    /// </summary>
    public static async Task<(HttpResponseMessage Response, string? Otp)> SendOtpAndExtractCodeAsync(
        this HttpClient client,
        IReadOnlyList<Fakes.SmsFake.SentMessage> smsSent,
        string phone)
    {
        var countBefore = smsSent.Count;
        
        var req = new { phone };
        var response = await client.PostAsJsonAsync("/api/v1/auth/send-otp", req);

        // صبر کوتاه برای اطمینان از ثبت در SmsFake
        await Task.Delay(100);

        // استخراج OTP
        string? otp = null;
        if (response.IsSuccessStatusCode && smsSent.Count > countBefore)
        {
            otp = smsSent
                .Where(m => NormalizePhone(m.Phone) == NormalizePhone(phone))
                .OrderByDescending(m => m.SentAtUtc)
                .FirstOrDefault()?.Code;
        }

        return (response, otp);
    }

    /// <summary>
    /// ارسال OTP، استخراج کد، و وریفای کردن
    /// </summary>
    public static async Task<(HttpResponseMessage VerifyResponse, LoginResponseDto? LoginResult)> 
        SendAndVerifyOtpAsync(
            this HttpClient client,
            IReadOnlyList<Fakes.SmsFake.SentMessage> smsSent,
            string? phone = null)
    {
        phone ??= GenerateUniquePhone();

        var (sendResponse, otp) = await client.SendOtpAndExtractCodeAsync(smsSent, phone);
        
        if (!sendResponse.IsSuccessStatusCode || string.IsNullOrWhiteSpace(otp))
        {
            return (sendResponse, null);
        }

        var verifyReq = new { phone, code = otp };
        var verifyResponse = await client.PostAsJsonAsync("/api/v1/auth/verify-otp", verifyReq);

        var loginResult = await verifyResponse.ExtractLoginResponseAsync();
        
        return (verifyResponse, loginResult);
    }

    /// <summary>
    /// نرمال‌سازی شماره تلفن (حذف کاراکترهای غیرعددی)
    /// </summary>
    private static string NormalizePhone(string phone)
    {
        return new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
    }
}

