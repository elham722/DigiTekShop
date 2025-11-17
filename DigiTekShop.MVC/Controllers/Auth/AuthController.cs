using DigiTekShop.MVC.Models.Auth;

namespace DigiTekShop.MVC.Controllers.Auth;

/// <summary>
/// کنترلر احراز هویت - فقط مدیریت کوکی‌های JWT
/// منطق اصلی احراز هویت در Backend API است
/// </summary>
[Route("[controller]/[action]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController : Controller
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    #region Login&Register

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = NormalizeReturnUrl(returnUrl);
        return View();
    }

    #endregion

    /// <summary>
    /// ست کردن کوکی‌های احراز هویت (Access/Refresh Token)
    /// این اکشن بعد از VerifyOtp از سمت JS صدا زده می‌شود.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [IgnoreAntiforgeryToken] // فعلاً CSRF برای JSON endpoints غیرفعال است
    public IActionResult SetAuthCookie([FromBody] SetAuthCookieRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            _logger.LogWarning("SetAuthCookie: AccessToken is missing");
            return BadRequest(new { success = false, message = "AccessToken is required" });
        }

        DateTimeOffset? accessTokenExpires = null;

        try
        {
            // فقط برای گرفتن exp از روی توکن (بدون validate امضا)
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(request.AccessToken))
            {
                var jwt = handler.ReadJwtToken(request.AccessToken);
                accessTokenExpires = jwt.ValidTo;
            }
        }
        catch (Exception ex)
        {
            // اگر parsing شکست خورد، فقط log کن؛ API خودش توکن رو validate کرده
            _logger.LogWarning(ex, "SetAuthCookie: Failed to read JWT token, using default expiration.");
        }

        // اگر exp نداشت، یه fallback منطقی
        var accessCookieExpires = accessTokenExpires ?? DateTimeOffset.UtcNow.AddMinutes(60);

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = accessCookieExpires
        };

        // کوکی AccessToken
        Response.Cookies.Append("dt_at", request.AccessToken, accessCookieOptions);

        // اگر RefreshToken داری، ست کن
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30) // مطابق تنظیمات API
            };

            Response.Cookies.Append("dt_rt", request.RefreshToken!, refreshCookieOptions);
        }

        var returnUrl = NormalizeReturnUrl(request.ReturnUrl);

        _logger.LogInformation("Auth cookies set successfully.");

        return Ok(new
        {
            success = true,
            message = "ورود با موفقیت انجام شد",
            redirectUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl,
            isNewUser = request.IsNewUser
        });
    }

    /// <summary>
    /// خروج کاربر از UI (پاک کردن کوکی‌ها)
    /// API Logout در فرانت (JS) قبل از این اکشن صدا زده می‌شود.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken] // فعلاً CSRF برای JSON endpoints غیرفعال است
    public IActionResult Logout()
    {
        _logger.LogInformation("User logging out from MVC client.");

        // پاک کردن کوکی‌های احراز هویت
        Response.Cookies.Delete("dt_at");
        Response.Cookies.Delete("dt_rt");

        // اگر کوکی‌های دیگری مثل device-id یا ... داری و می‌خوای پاک شوند، این‌جا اضافه کن

        return Ok(new
        {
            success = true,
            message = "خروج با موفقیت انجام شد",
            redirectUrl = "/Auth/Login"
        });
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Success()
    {
        return View();
    }

    #region Helpers

    private string NormalizeReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        return Url.IsLocalUrl(url) ? url : "/";
    }

    #endregion
}
