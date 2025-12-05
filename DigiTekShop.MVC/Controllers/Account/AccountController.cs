namespace DigiTekShop.MVC.Controllers.Account;

[Route("[controller]/[action]")]
public sealed class AccountController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }


    [HttpGet]
    public IActionResult Profile()
    {
        return View();
    }


  
}

