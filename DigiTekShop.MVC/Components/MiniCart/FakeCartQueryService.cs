namespace DigiTekShop.MVC.Components.MiniCart;
public sealed class FakeCartQueryService : ICartQueryService
{
    public Task<MiniCartViewModel> GetMiniAsync(HttpContext ctx)
    {
        var items = new List<MiniCartItemVm>
        {
            new("iPhone 13 A2634 128GB", "apple", "/theme-assets/images/products/01.jpg", "/product/iphone-13", 1, 26249000, "#d4d4d4"),
            new("Xiaomi 11 Lite 5G NE 256GB", "xiaomi", "/theme-assets/images/products/02.jpg", "/product/xiaomi-11-lite", 1, 8239000, "#d4d4d4"),
            new("iPhone 12 Pro Max 256GB", "apple", "/theme-assets/images/products/05.jpg", "/product/iphone-12-pro-max", 1, 36300000, "#d4d4d4"),
            new("Galaxy S9 Plus 64GB", "samsung", "/theme-assets/images/products/07.jpg", "/product/galaxy-s9-plus", 1, 12890000, "#d4d4d4"),
        };
        return Task.FromResult(new MiniCartViewModel(items));
    }
}
