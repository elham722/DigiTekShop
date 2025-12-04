namespace DigiTekShop.Contracts.Abstractions.Identity.Profile;

public interface IUserProfileService
{
    Task<Result> LinkCustomerToUserAsync(Guid userId, Guid customerId, CancellationToken ct = default);

    Task<bool> HasProfileAsync(Guid userId, CancellationToken ct = default);
}

