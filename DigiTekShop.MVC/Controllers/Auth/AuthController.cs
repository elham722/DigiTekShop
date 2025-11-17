namespace DigiTekShop.MVC.Controllers.Auth;

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


    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
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
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(request.AccessToken))
            {
                var jwt = handler.ReadJwtToken(request.AccessToken);
                accessTokenExpires = jwt.ValidTo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SetAuthCookie: Failed to read JWT token, using default expiration.");
        }

        var accessCookieExpires = accessTokenExpires ?? DateTimeOffset.UtcNow.AddMinutes(60);

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = accessCookieExpires
        };

        Response.Cookies.Append("dt_at", request.AccessToken, accessCookieOptions);

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30) 
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

    [HttpPost]
    public IActionResult Logout()
    {
        _logger.LogInformation("User logging out from MVC client.");

        Response.Cookies.Delete("dt_at");
        Response.Cookies.Delete("dt_rt");

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
