namespace DigiTekShop.MVC.Components.UserMenu;
public sealed class FakeCurrentUserProfileService : ICurrentUserProfileService
{
    public Task<CurrentUserProfileVm> GetAsync()
        => Task.FromResult(new CurrentUserProfileVm(
            "جلال بهرامی‌راد", "09xxxxxxxxx", "/theme-assets/images/avatar/default.png"));
}
