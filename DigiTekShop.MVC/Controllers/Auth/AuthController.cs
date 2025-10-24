using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;
using DigiTekShop.Contracts.DTOs.Auth.Logout;
using Microsoft.AspNetCore.Authentication;
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
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = NormalizeReturnUrl(returnUrl);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _api.PostAsync<SendOtpRequestDto, object>(ApiRoutes.Auth.SentOtp, dto, ct);
        if (!result.Success)
        {
            _logger.LogWarning("SendOtp failed: {Status} {Detail}", (int)result.StatusCode, result.Problem?.Detail);
            return StatusCode((int)result.StatusCode, result.Problem);
        }

        return Ok(new { message = "OTP sent successfully" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto, [FromQuery] string? returnUrl, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _api.PostAsync<VerifyOtpRequestDto, LoginResponseDto>(ApiRoutes.Auth.VerifyOtp, dto, ct);
        if (!result.Success || result.Data is null)
        {
            _logger.LogWarning("VerifyOtp failed: {Status} {Detail}", (int)result.StatusCode, result.Problem?.Detail);
            return StatusCode((int)result.StatusCode, result.Problem);
        }

        var login = result.Data;

        
        var principal = BuildPrincipalFromLogin(login);

        
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = login.AccessTokenExpiresAtUtc   
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            props);

        await _tokenStore.UpdateAccessTokenAsync(login.AccessToken, login.AccessTokenExpiresAtUtc, ct);

        var safeReturn = NormalizeReturnUrl(returnUrl);
        return Ok(new
        {
            message = "Login successful",
            redirectUrl = string.IsNullOrEmpty(safeReturn) ? "/" : safeReturn,
            isNewUser = login.IsNewUser
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("application/json")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest dto, CancellationToken ct)
    {
        var res = await _api.PostAsync<LogoutRequest>($"{ApiRoutes.Auth.Logout}", dto, ct);
        if (!res.Success)
        {
            _logger.LogWarning("Logout (API) failed: {Status} {Detail}", (int)res.StatusCode, res.Problem?.Detail);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session?.Clear();
        return NoContent();
    }

    [HttpGet]
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
            new Claim("access_token", login.AccessToken)
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
