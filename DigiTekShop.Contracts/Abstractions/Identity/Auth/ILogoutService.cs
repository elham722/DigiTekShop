namespace DigiTekShop.Contracts.Abstractions.Identity.Auth;
public interface ILogoutService
{
    Task<Result> LogoutAsync(LogoutRequest dto, CancellationToken ct);
    Task<Result> LogoutAllAsync(LogoutAllRequest dto, CancellationToken ct);
}