using DigiTekShop.Contracts.DTOs.User;
using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Contracts.Abstractions.Identity.Security;
public interface IIdentityGateway
{
    Task<AppUser?> FindByLoginAsync(string login, CancellationToken ct);
    Task<AppUser?> FindByIdAsync(Guid userId, CancellationToken ct);

    Task<bool> CheckPasswordAsync(AppUser user, string password, CancellationToken ct);
    Task<bool> IsLockedOutAsync(AppUser user, CancellationToken ct);
    Task AccessFailedAsync(AppUser user, CancellationToken ct);
    bool CanSignIn(AppUser user);

    Task<bool> IsMfaRequiredAsync(AppUser user, CancellationToken ct);
    Task<IReadOnlyList<MfaMethod>> GetAvailableMfaMethodsAsync(AppUser user, CancellationToken ct);
    Task<bool> VerifyTotpAsync(AppUser user, string code, CancellationToken ct);
    Task<bool> VerifySecondFactorAsync(AppUser user, MfaMethod method, string code, CancellationToken ct);

    Task UniformDelayAsync(CancellationToken ct); 
}
