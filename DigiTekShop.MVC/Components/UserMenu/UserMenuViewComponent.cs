namespace DigiTekShop.MVC.Components.UserMenu;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

public sealed class UserMenuViewComponent : ViewComponent
{
    private readonly ICurrentUserProfileService _profile;

    public UserMenuViewComponent(ICurrentUserProfileService profile)
        => _profile = profile;

    public async Task<IViewComponentResult> InvokeAsync(string? variant = null)
    {
        var isAuth = User?.Identity?.IsAuthenticated == true;

        // انتخاب ویوی موبایل/دسکتاپ بر اساس variant
        if (!isAuth)
        {
            return variant?.Equals("mobile", StringComparison.OrdinalIgnoreCase) == true
                ? View("MobileAnonymous")
                : View("Anonymous");
        }

        var vm = await _profile.GetAsync();

        return variant?.Equals("mobile", StringComparison.OrdinalIgnoreCase) == true
            ? View("MobileDefault", vm)
            : View("Default", vm);
    }
}

public sealed record CurrentUserProfileVm(string FullName, string? Phone, string? AvatarUrl);

public interface ICurrentUserProfileService
{
    Task<CurrentUserProfileVm> GetAsync();
}
