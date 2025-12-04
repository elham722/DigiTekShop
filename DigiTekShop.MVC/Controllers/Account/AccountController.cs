namespace DigiTekShop.MVC.Controllers.Account;

[Route("[controller]/[action]")]
public sealed class AccountController : Controller
{  
    [HttpGet]
    public IActionResult Profile()
    {
        return View();
    }


    [HttpGet]
    public IActionResult CompleteProfile()
    {
        return View();
    }
}

