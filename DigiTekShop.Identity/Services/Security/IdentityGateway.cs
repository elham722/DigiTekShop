using DigiTekShop.Contracts.Abstractions.Identity.Security;
using DigiTekShop.Contracts.DTOs.User;
using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Identity.Services.Security;

public sealed class IdentityGateway : IIdentityGateway
{
    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;

    public IdentityGateway(UserManager<User> users, SignInManager<User> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    public async Task<AppUser?> FindByLoginAsync(string login, CancellationToken ct)
    {
        User? u = login.Contains('@')
            ? await _users.FindByEmailAsync(login)
            : await _users.FindByNameAsync(login);

        return u is null ? null : Map(u);
    }

    public async Task<AppUser?> FindByIdAsync(Guid userId, CancellationToken ct)
    {
        var u = await _users.FindByIdAsync(userId.ToString());
        return u is null ? null : Map(u);
    }

    public async Task<bool> CheckPasswordAsync(AppUser user, string password, CancellationToken ct)
    {
        var u = await _users.FindByIdAsync(user.Id.ToString());
        return u is not null && await _users.CheckPasswordAsync(u, password);
    }

    public async Task<bool> IsLockedOutAsync(AppUser user, CancellationToken ct)
    {
        var u = await _users.FindByIdAsync(user.Id.ToString());
        return u is not null && await _users.IsLockedOutAsync(u);
    }

    public async Task AccessFailedAsync(AppUser user, CancellationToken ct)
    {
        var u = await _users.FindByIdAsync(user.Id.ToString());
        if (u is not null) await _users.AccessFailedAsync(u);
    }

    public bool CanSignIn(AppUser user)
    {
        return user.EmailConfirmed;
    }

    public async Task<bool> IsMfaRequiredAsync(AppUser user, CancellationToken ct)
    {
        var u = await _users.FindByIdAsync(user.Id.ToString());
        if (u is null) return false;
        return await _users.GetTwoFactorEnabledAsync(u);
    }

    public async Task<IReadOnlyList<MfaMethod>> GetAvailableMfaMethodsAsync(AppUser user, CancellationToken ct)
    {
        var u = await _users.FindByIdAsync(user.Id.ToString());
        if (u is null) return Array.Empty<MfaMethod>();

        var factors = new List<MfaMethod>();
        var providers = await _users.GetValidTwoFactorProvidersAsync(u);
        if (providers.Contains(TokenOptions.DefaultAuthenticatorProvider))
            factors.Add(MfaMethod.Totp);
        if (providers.Contains("Email")) factors.Add(MfaMethod.Email);
        if (providers.Contains("Phone")) factors.Add(MfaMethod.Sms);
        return factors;
    }

    public async Task<bool> VerifyTotpAsync(AppUser user, string code, CancellationToken ct)
    {
        var u = await _users.FindByIdAsync(user.Id.ToString());
        if (u is null) return false;

        var ok = await _users.VerifyTwoFactorTokenAsync(
            u, TokenOptions.DefaultAuthenticatorProvider, code);
        return ok;
    }

    public async Task<bool> VerifySecondFactorAsync(AppUser user, MfaMethod method, string code, CancellationToken ct)
    {
        return method switch
        {
            MfaMethod.Totp => await VerifyTotpAsync(user, code, ct),
            MfaMethod.Email => await VerifyWithProvider(user, "Email", code),
            MfaMethod.Sms => await VerifyWithProvider(user, "Phone", code),
            _ => false
        };
    }

    public Task UniformDelayAsync(CancellationToken ct)
    {
        return Task.Delay(Random.Shared.Next(120, 200), ct);
    }

    #region Helpers

    private static AppUser Map(User u) =>
        new(u.Id, u.Email!, u.UserName, u.EmailConfirmed);

    private async Task<bool> VerifyWithProvider(AppUser user, string provider, string code)
    {
        var u = await _users.FindByIdAsync(user.Id.ToString());
        if (u is null) return false;
        return await _users.VerifyTwoFactorTokenAsync(u, provider, code);
    }

    #endregion

}

