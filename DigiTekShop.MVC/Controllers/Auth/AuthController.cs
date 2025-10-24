namespace DigiTekShop.MVC.Controllers.Auth;

public sealed class AuthController(IApiClient api) : Controller
{
    private readonly IApiClient _api = api;

    public IActionResult Login()
    {
        return View();
    }
}
