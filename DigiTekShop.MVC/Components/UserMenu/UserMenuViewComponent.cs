namespace DigiTekShop.MVC.Components.UserMenu;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

public sealed class UserMenuViewComponent : ViewComponent
{
    private readonly ICurrentUserProfileService _profile;

    public UserMenuViewComponent(ICurrentUserProfileService profile)
        => _profile = profile;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return View("Anonymous");

        var vm = await _profile.GetAsync(); // نام/شماره/آواتار
        return View(vm);
    }
}

public sealed record CurrentUserProfileVm(string FullName, string? Phone, string? AvatarUrl);

public interface ICurrentUserProfileService
{
    Task<CurrentUserProfileVm> GetAsync();
}
