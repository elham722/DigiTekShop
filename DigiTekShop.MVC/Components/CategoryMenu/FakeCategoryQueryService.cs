namespace DigiTekShop.MVC.Components.CategoryMenu;

public sealed class FakeCategoryQueryService : ICategoryQueryService
{
    public Task<IReadOnlyList<CategoryNodeVm>> GetForHeaderAsync(int depth)
    {
        var tree = (IReadOnlyList<CategoryNodeVm>)new List<CategoryNodeVm>
        {
            new("کالای دیجیتال", "/c/digital", new[]
            {
                new CategoryNodeVm("لوازم جانبی گوشی", "/c/phone-accessories"),
                new CategoryNodeVm("گوشی موبایل", "/c/phones", new[]
                {
                    new CategoryNodeVm("سامسونگ", "/c/phones/samsung"),
                    new CategoryNodeVm("اپل", "/c/phones/apple"),
                    new CategoryNodeVm("شیائومی", "/c/phones/xiaomi"),
                }),
                new CategoryNodeVm("مچ‌بند و ساعت هوشمند", "/c/wearables"),
            }),
            new("مد و پوشاک", "/c/fashion", new[]
            {
                new CategoryNodeVm("زنانه", "/c/fashion/women"),
                new CategoryNodeVm("مردانه", "/c/fashion/men"),
            }),
            new("اسباب‌بازی", "/c/toys"),
            new("زیبایی و سلامت", "/c/beauty"),
            new("خانه و آشپزخانه", "/c/home"),
            new("ورزش و سفر", "/c/sport"),
            new("سوپرمارکت", "/c/supermarket"),
        };
        return Task.FromResult(tree);
    }
}

