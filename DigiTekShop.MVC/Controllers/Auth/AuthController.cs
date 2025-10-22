namespace DigiTekShop.MVC.Controllers.Auth;

public sealed class AuthController(IApiClient api) : Controller
{
    private readonly IApiClient _api = api;

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(); 
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Login([FromForm] LoginRequest dto, string? returnUrl = null, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return View(dto);

        var res = await _api.PostAsync<LoginRequest, LoginResultDto>(ApiRoutes.Auth.Login, dto, ct);

        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError(string.Empty, res.Problem?.Detail ?? res.Problem?.Title ?? "Login failed");
            return View(dto);
        }

        if (res.Data.IsChallenge && res.Data.Challenge is not null)
        {
            var challenge = res.Data.Challenge;

            var mfaVm = new VerifyMfaRequest
            {
                UserId = challenge.UserId,
                Method = challenge.Methods.Contains(MfaMethod.Totp) ? MfaMethod.Totp : challenge.Methods.FirstOrDefault(),
                Code = string.Empty,
                TrustThisDevice = dto.RememberMe 
            };

            ViewBag.Methods = challenge.Methods;
            ViewBag.ReturnUrl = returnUrl;

            return View("Mfa", mfaVm);
        }

        if (res.Data.IsSuccess && res.Data.Success is not null)
        {
            await SignInWithAccessTokenAsync(res.Data.Success.AccessToken);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Unexpected login state.");
        return View(dto);
    }

    
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Mfa(Guid userId, string? returnUrl = null)
    {
        var vm = new VerifyMfaRequest { UserId = userId, Method = MfaMethod.Totp };
        ViewBag.ReturnUrl = returnUrl;
        return View(vm);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> VerifyMfa([FromForm] VerifyMfaRequest dto, string? returnUrl = null, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View("Mfa", dto);
        }

        var res = await _api.PostAsync<VerifyMfaRequest, LoginResponse>(ApiRoutes.Auth.VerifyMfa, dto, ct);

        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError(string.Empty, res.Problem?.Detail ?? res.Problem?.Title ?? "MFA verification failed");
            ViewBag.ReturnUrl = returnUrl;
            return View("Mfa", dto);
        }

        await SignInWithAccessTokenAsync(res.Data.AccessToken);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _api.PostAsync<object>(ApiRoutes.Auth.Logout, new { }, ct);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    #region Helpers

    private async Task SignInWithAccessTokenAsync(string accessToken)
    {
        var claims = new List<Claim> { new("access_token", accessToken) };

        var handler = new JwtSecurityTokenHandler();
        if (handler.CanReadToken(accessToken))
        {
            var jwt = handler.ReadJwtToken(accessToken);
            foreach (var c in jwt.Claims)
            {
                if (c.Type is ClaimTypes.Name or ClaimTypes.NameIdentifier or ClaimTypes.Email or ClaimTypes.Role)
                    claims.Add(c);
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = TryGetJwtExpiry(accessToken),
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    private static DateTimeOffset? TryGetJwtExpiry(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var exp = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (exp != null && long.TryParse(exp, out var seconds))
                return DateTimeOffset.FromUnixTimeSeconds(seconds).ToUniversalTime();
        }
        catch { /* ignore */ }
        return null;
    }

    #endregion
}
