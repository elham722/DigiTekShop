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
    [ValidateAntiForgeryToken]
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
                // Try to get retryAfter from headers or fallback to 30
                var retryAfter = "30";
                if (result.Problem?.Extensions?.TryGetValue("retryAfter", out var ra) == true)
                {
                    retryAfter = ra?.ToString() ?? "30";
                }
                else if (result.Problem?.Extensions?.TryGetValue("limit", out var limit) == true)
                {
                    // Fallback: use window seconds if available
                    if (result.Problem.Extensions.TryGetValue("window", out var window))
                    {
                        retryAfter = window?.ToString() ?? "30";
                    }
                }
                
                return this.JsonError($"درخواست زیاد بود. لطفاً {retryAfter} ثانیه صبر کنید و دوباره تلاش کنید.", 
                    new { retryAfter = int.Parse(retryAfter) });
            }
            
            return this.JsonError("خطا در ارسال کد. لطفاً دوباره تلاش کنید");
        }

        return this.JsonSuccess("کد تأیید با موفقیت ارسال شد");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto, [FromQuery] string? returnUrl, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return this.JsonValidationError("لطفاً اطلاعات را صحیح وارد کنید", ModelState);

        var result = await _api.PostAsync<VerifyOtpRequestDto, LoginResponseDto>(ApiRoutes.Auth.VerifyOtp, dto, ct);
        if (!result.Success || result.Data is null)
        {
            _logger.LogWarning("VerifyOtp failed: {Status} {Detail}", (int)result.StatusCode, result.Problem?.Detail);
            
            // Handle 429 Rate Limiting specially
            if (result.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // Try to get retryAfter from headers or fallback to 30
                var retryAfter = "30";
                if (result.Problem?.Extensions?.TryGetValue("retryAfter", out var ra) == true)
                {
                    retryAfter = ra?.ToString() ?? "30";
                }
                else if (result.Problem?.Extensions?.TryGetValue("limit", out var limit) == true)
                {
                    // Fallback: use window seconds if available
                    if (result.Problem.Extensions.TryGetValue("window", out var window))
                    {
                        retryAfter = window?.ToString() ?? "30";
                    }
                }
                
                return this.JsonError($"درخواست زیاد بود. لطفاً {retryAfter} ثانیه صبر کنید و دوباره تلاش کنید.", 
                    new { retryAfter = int.Parse(retryAfter) });
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

            await _tokenStore.UpdateAccessTokenAsync(login.AccessToken, login.AccessTokenExpiresAtUtc, ct);

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
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest dto, CancellationToken ct)
    {
        var res = await _api.PostAsync<LogoutRequest>(ApiRoutes.Auth.Logout, dto, ct);
        if (!res.Success)
            _logger.LogWarning("Logout (API) failed: {Status} {Detail}", (int)res.StatusCode, res.Problem?.Detail);

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

    // ----------------- Helpers -----------------

    private ClaimsPrincipal BuildPrincipalFromLogin(LoginResponseDto login)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, login.UserId.ToString()),
            new Claim(ClaimTypes.Name, "User"),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private string NormalizeReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        return Url.IsLocalUrl(url) ? url : "/";
    }
}
