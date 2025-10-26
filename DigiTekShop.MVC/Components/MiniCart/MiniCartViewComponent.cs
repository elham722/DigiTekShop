namespace DigiTekShop.MVC.Components.MiniCart;
using Microsoft.AspNetCore.Mvc;

public sealed class MiniCartViewComponent : ViewComponent
{
    private readonly ICartQueryService _cart;

    public MiniCartViewComponent(ICartQueryService cart) => _cart = cart;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var vm = await _cart.GetMiniAsync(HttpContext);
        return View(vm);
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

