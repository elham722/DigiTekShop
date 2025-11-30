using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.MVC.Controllers.Account;

/// <summary>
/// حساب کاربری - فقط رندر View
/// تمام دیتا از API با JS می‌آید
/// </summary>
[Route("[controller]/[action]")]
public sealed class AccountController : Controller
{
    /// <summary>
    /// صفحه پروفایل کاربر
    /// GET /account/profile
    /// </summary>
    [HttpGet]
    public IActionResult Profile()
    {
        return View();
    }
}

