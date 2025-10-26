namespace DigiTekShop.MVC.Components.MobileCategoryMenu;

using DigiTekShop.MVC.Components.CategoryMenu;
using Microsoft.AspNetCore.Mvc;

public sealed class MobileCategoryMenuViewComponent : ViewComponent
{
    private readonly ICategoryQueryService _svc;
    public MobileCategoryMenuViewComponent(ICategoryQueryService svc) => _svc = svc;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var tree = await _svc.GetForHeaderAsync(depth: 3);
        return View(tree);
    }
}


