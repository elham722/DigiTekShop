using DigiTekShop.MVC.Services;
using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;
using Microsoft.AspNetCore.Mvc;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using Microsoft.AspNetCore.Authentication;

namespace DigiTekShop.MVC.Controllers.Auth;

[Route("[controller]/[action]")]
public sealed class AuthController : Controller
{
    private readonly IApiClient _api;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IApiClient api, ILogger<AuthController> logger)
    {
        _api = api;
        _logger = logger;
    }

    // صفحه اصلی ورود
    [HttpGet]
    public IActionResult Login() => View();

    // مرحله اول - ارسال OTP
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto dto, CancellationToken ct)
    {
        var result = await _api.PostAsync<SendOtpRequestDto, object>("api/v1/auth/send-otp", dto, ct);
        if (!result.Success)
        {
            _logger.LogWarning("SendOtp failed: {Detail}", result.Problem?.Detail);
            return StatusCode((int)result.StatusCode, result.Problem);
        }

        return Ok(new { message = "OTP sent successfully" });
    }

    // مرحله دوم - تأیید OTP
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto, CancellationToken ct)
    {
        var result = await _api.PostAsync<VerifyOtpRequestDto, LoginResponseDto>("api/v1/auth/verify-otp", dto, ct);

        if (!result.Success)
        {
            _logger.LogWarning("VerifyOtp failed: {Detail}", result.Problem?.Detail);
            return StatusCode((int)result.StatusCode, result.Problem);
        }

        // ذخیره توکن‌ها در سشن/کوکی (اختیاری – اگه لازم داری)
        HttpContext.Session.SetString("AccessToken", result.Data!.AccessToken);
        HttpContext.Session.SetString("RefreshToken", result.Data.RefreshToken);

        return Ok(result.Data);
    }

    // صفحه موفقیت (بعد از ورود)
    [HttpGet]
    public IActionResult Success() => View();

    // خروج کاربر
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest dto, CancellationToken ct)
    {
        var result = await _api.PostAsync<LogoutRequest>("api/v1/auth/logout", dto, ct);
        if (!result.Success)
        {
            _logger.LogWarning("Logout failed: {Detail}", result.Problem?.Detail);
            return StatusCode((int)result.StatusCode, result.Problem);
        }

        // پاک کردن سشن و کوکی‌ها
        await HttpContext.SignOutAsync();
        HttpContext.Session.Clear();

        return NoContent();
    }
}
