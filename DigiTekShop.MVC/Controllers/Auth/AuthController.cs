using DigiTekShop.MVC.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace DigiTekShop.MVC.Controllers.Auth;

/// <summary>
/// Controller نازک برای مدیریت UI Authentication
/// همه API calls از طریق JavaScript → YARP → Backend API
/// این controller فقط برای View و Cookie management است
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

    /// <summary>
    /// نمایش صفحه Login (فقط View)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = NormalizeReturnUrl(returnUrl);
        return View();
    }

    /// <summary>
    /// بعد از موفقیت VerifyOtp در API، JavaScript این endpoint را صدا می‌زند
    /// تا Cookie برای UI ست شود
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    public async Task<IActionResult> SetAuthCookie([FromBody] SetAuthCookieRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            _logger.LogWarning("SetAuthCookie: AccessToken is missing");
            return BadRequest(new { success = false, message = "AccessToken is required" });
        }

        try
        {
            // Parse JWT token برای گرفتن Claims
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(request.AccessToken))
            {
                _logger.LogWarning("SetAuthCookie: Invalid JWT token format");
                return BadRequest(new { success = false, message = "Invalid token format" });
            }

            var jwt = handler.ReadJwtToken(request.AccessToken);
            var claims = jwt.Claims.ToList();

            // اطمینان از وجود NameIdentifier (userId)
            if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            {
                var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sub");
                if (subClaim != null)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = jwt.ValidTo
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            var returnUrl = NormalizeReturnUrl(request.ReturnUrl);
            _logger.LogInformation("User authenticated successfully (sub={Sub})", jwt.Subject);

            return Ok(new
            {
                success = true,
                message = "ورود با موفقیت انجام شد",
                redirectUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl,
                isNewUser = request.IsNewUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting auth cookie");
            return StatusCode(500, new { success = false, message = "خطا در فرآیند ورود" });
        }
    }

    /// <summary>
    /// Logout: پاک کردن Cookie UI
    /// JavaScript باید قبل/بعد، API logout را هم صدا بزند
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User logging out (userId={UserId})", userId);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session?.Clear();

        return Ok(new { success = true, message = "خروج با موفقیت انجام شد", redirectUrl = "/Auth/Login" });
    }

    /// <summary>
    /// صفحه AccessDenied
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// صفحه Success بعد از Login
    /// </summary>
    [HttpGet]
    [Authorize]
    public IActionResult Success()
    {
        return View();
    }

    // ----------------- Helpers -----------------

    private string NormalizeReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        return Url.IsLocalUrl(url) ? url : "/";
    }
}

/// <summary>
/// Request model برای SetAuthCookie
/// این اطلاعات بعد از موفقیت VerifyOtp در JavaScript از API دریافت و به این endpoint ارسال می‌شود
/// </summary>
public sealed record SetAuthCookieRequest
{
    public required string AccessToken { get; init; }
    public string? ReturnUrl { get; init; }
    public bool IsNewUser { get; init; }
}
