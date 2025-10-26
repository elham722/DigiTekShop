namespace DigiTekShop.MVC.Components.MiniCart;
using Microsoft.AspNetCore.Mvc;

public sealed class MiniCartViewComponent : ViewComponent
{
    private readonly ICartQueryService _cart;

    public MiniCartViewComponent(ICartQueryService cart) => _cart = cart;

    public async Task<IViewComponentResult> InvokeAsync(string? variant = null)
    {
        var vm = await _cart.GetMiniAsync(HttpContext);

        if (variant?.Equals("mobile", StringComparison.OrdinalIgnoreCase) == true)
            return View("Mobile", vm);   // Views/Shared/Components/MiniCart/Mobile.cshtml

        return View("Default", vm);      // Views/Shared/Components/MiniCart/Default.cshtml
    }
}

public interface ICartQueryService
{
    Task<MiniCartViewModel> GetMiniAsync(HttpContext ctx);
}

public sealed record MiniCartViewModel(IReadOnlyList<MiniCartItemVm> Items)
{
    public int Count => Items.Count;
    public long Total => Items.Sum(i => i.Price * i.Quantity);
}
public sealed record MiniCartItemVm(string Title, string Brand, string ImageUrl, string Link, int Quantity, long Price, string? ColorHex);

