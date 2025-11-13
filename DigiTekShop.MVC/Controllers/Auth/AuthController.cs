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

    [HttpGet()]
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
    public async Task<IActionResult> SetAuthCookie([FromBody] SetAuthCookieRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            _logger.LogWarning("SetAuthCookie: AccessToken is missing");
            return BadRequest(new { success = false, message = "AccessToken is required" });
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(request.AccessToken))
            {
                _logger.LogWarning("SetAuthCookie: Invalid JWT token format");
                return BadRequest(new { success = false, message = "Invalid token format" });
            }

            var jwt = handler.ReadJwtToken(request.AccessToken);
            var claims = jwt.Claims.ToList();

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

    [HttpGet]
    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
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
