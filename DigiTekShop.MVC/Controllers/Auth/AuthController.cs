using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using DigiTekShop.MVC.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Security.Claims;

namespace DigiTekShop.MVC.Controllers.Auth;

[Route("[controller]/[action]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController : Controller
{
    private readonly IApiClient _api;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IApiClient api, ITokenStore tokenStore, ILogger<AuthController> logger)
    {
        _api = api;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = NormalizeReturnUrl(returnUrl);
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return this.JsonValidationError("لطفاً اطلاعات را صحیح وارد کنید", ModelState);

        var result = await _api.PostAsync<SendOtpRequestDto, object>(ApiRoutes.Auth.SendOtp, dto, ct);
        if (!result.Success)
        {
            _logger.LogWarning("SendOtp failed: {Status} {Detail}", (int)result.StatusCode, result.Problem?.Detail);
            
            // Handle 429 Rate Limiting specially
            if (result.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // اول هدر Retry-After را چک کن
                int retryAfter = 30;
                if (result.Headers?.RetryAfter?.Delta is { } delta && delta.TotalSeconds > 0)
                {
                    retryAfter = (int)delta.TotalSeconds;
                }
                else if (result.Headers?.RetryAfter?.Date is { } date)
                {
                    var seconds = (int)(date - DateTimeOffset.UtcNow).TotalSeconds;
                    if (seconds > 0) retryAfter = seconds;
                }
                else if (result.Problem?.Extensions?.TryGetValue("retryAfter", out var ra) == true)
                {
                    if (int.TryParse(ra?.ToString(), out var parsed)) retryAfter = parsed;
                }
                else if (result.Problem?.Extensions?.TryGetValue("window", out var window) == true)
                {
                    if (int.TryParse(window?.ToString(), out var parsed)) retryAfter = parsed;
                }
                
                // Return 429 status code
                Response.StatusCode = 429;
                return this.JsonError($"درخواست زیاد بود. لطفاً {retryAfter} ثانیه صبر کنید و دوباره تلاش کنید.", 
                    new { retryAfter });
            }
            
            return this.JsonError("خطا در ارسال کد. لطفاً دوباره تلاش کنید");
        }

        return this.JsonSuccess("کد تأیید با موفقیت ارسال شد");
    }

    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto, CancellationToken ct)
    {
        // ReturnUrl را از Query String یا TempData بگیر
        var returnUrl = Request.Query["returnUrl"].ToString();
        if (string.IsNullOrWhiteSpace(returnUrl))
            returnUrl = TempData["ReturnUrl"]?.ToString();

        if (!ModelState.IsValid)
            return this.JsonValidationError("لطفاً اطلاعات را صحیح وارد کنید", ModelState);

        var result = await _api.PostAsync<VerifyOtpRequestDto, LoginResponseDto>(ApiRoutes.Auth.VerifyOtp, dto, ct);
        if (!result.Success || result.Data is null)
        {
            _logger.LogWarning("VerifyOtp failed: {Status} {Detail}", (int)result.StatusCode, result.Problem?.Detail);
            
            // Handle 429 Rate Limiting specially
            if (result.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // اول هدر Retry-After را چک کن
                int retryAfter = 30;
                if (result.Headers?.RetryAfter?.Delta is { } delta && delta.TotalSeconds > 0)
                {
                    retryAfter = (int)delta.TotalSeconds;
                }
                else if (result.Headers?.RetryAfter?.Date is { } date)
                {
                    var seconds = (int)(date - DateTimeOffset.UtcNow).TotalSeconds;
                    if (seconds > 0) retryAfter = seconds;
                }
                else if (result.Problem?.Extensions?.TryGetValue("retryAfter", out var ra) == true)
                {
                    if (int.TryParse(ra?.ToString(), out var parsed)) retryAfter = parsed;
                }
                else if (result.Problem?.Extensions?.TryGetValue("window", out var window) == true)
                {
                    if (int.TryParse(window?.ToString(), out var parsed)) retryAfter = parsed;
                }
                
                // Return 429 status code
                Response.StatusCode = 429;
                return this.JsonError($"درخواست زیاد بود. لطفاً {retryAfter} ثانیه صبر کنید و دوباره تلاش کنید.", 
                    new { retryAfter });
            }
            
            return this.JsonError("کد تأیید اشتباه است. لطفاً دوباره تلاش کنید");
        }

        var login = result.Data;

        try
        {
            var principal = BuildPrincipalFromLogin(login);
            var props = new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = login.AccessTokenExpiresAtUtc
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            // ذخیره AccessToken و RefreshToken
            await _tokenStore.UpdateTokensAsync(
                login.AccessToken, 
                login.AccessTokenExpiresAtUtc, 
                login.RefreshToken, 
                login.RefreshTokenExpiresAtUtc, 
                ct);

            var safeReturn = NormalizeReturnUrl(returnUrl);
            return this.JsonSuccess("ورود با موفقیت انجام شد", new
            {
                redirectUrl = string.IsNullOrEmpty(safeReturn) ? "/" : safeReturn,
                isNewUser = login.IsNewUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process");
            return this.JsonError("خطا در فرآیند ورود. لطفاً دوباره تلاش کنید");
        }
    }

    [HttpPost]
    [Authorize]
    [Consumes("application/json")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? dto, CancellationToken ct)
    {
        // UserId را از Claims بگیر (نه از Body) برای جلوگیری از confused deputy
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Logout: Invalid or missing UserId claim");
            return Unauthorized();
        }

        // اگر dto null است، از RefreshToken Cookie استفاده کن
        var refreshToken = dto?.RefreshToken ?? _tokenStore.GetRefreshToken();
        
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var logoutDto = new LogoutRequest { UserId = userId, RefreshToken = refreshToken };
            var res = await _api.PostAsync<LogoutRequest>(ApiRoutes.Auth.Logout, logoutDto, ct);
            if (!res.Success)
                _logger.LogWarning("Logout (API) failed: {Status} {Detail}", (int)res.StatusCode, res.Problem?.Detail);
        }

        // پاک کردن RefreshToken Cookie
        Response.Cookies.Delete("rt");
        
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session?.Clear();
        return NoContent();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _api.GetAsync<object>(ApiRoutes.Auth.Me, ct);
        if (!result.Success) return StatusCode((int)result.StatusCode, result.Problem);
        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ----------------- Helpers -----------------

    private ClaimsPrincipal BuildPrincipalFromLogin(LoginResponseDto login)
    {
        // Parse Claims از JWT برای همخوانی ۱۰۰٪ با API
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(login.AccessToken);
        var claims = jwt.Claims.ToList();

        // اطمینان از وجود NameIdentifier
        if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, login.UserId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private string NormalizeReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        return Url.IsLocalUrl(url) ? url : "/";
    }
}
