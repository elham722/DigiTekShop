namespace DigiTekShop.MVC.Components.NotificationTicker;
using Microsoft.AspNetCore.Mvc;

public sealed class NotificationTickerViewComponent : ViewComponent
{
    // می‌تونی از IOptions<HeaderLinksOptions> یا سرویس اختصاصی بخونی
    public IViewComponentResult Invoke()
    {
        var items = new[]
        {
            new NotificationItemVm("خوش آمدید 👋"),
            new NotificationItemVm("زمستان امسال با تخفیفات ویژه 😍"),
            new NotificationItemVm("به باشگاه مشتریان ما بپیوندید 😎")
        };
        return View(items);
    }
}

public sealed record NotificationItemVm(string Text);

