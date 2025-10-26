namespace DigiTekShop.MVC.Components.CategoryMenu;
using Microsoft.AspNetCore.Mvc;

public sealed class CategoryMenuViewComponent : ViewComponent
{
    private readonly ICategoryQueryService _svc;
    public CategoryMenuViewComponent(ICategoryQueryService svc) => _svc = svc;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var tree = await _svc.GetForHeaderAsync(depth: 3);
        return View(tree);
    }
}

public interface ICategoryQueryService
{
    Task<IReadOnlyList<CategoryNodeVm>> GetForHeaderAsync(int depth);
}

public sealed record CategoryNodeVm(
    string Title,
    string? Link,
    IReadOnlyList<CategoryNodeVm>? Children = null);

