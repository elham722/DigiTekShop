namespace DigiTekShop.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/[controller]/[action]")]
public sealed class UsersController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}

