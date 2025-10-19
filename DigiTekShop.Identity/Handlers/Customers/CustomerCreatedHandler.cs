using DigiTekShop.Contracts.Integration.Events.Customers;
using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Identity.Handlers.Customers;


public sealed class CustomerCreatedHandler : IIntegrationEventHandler<AddCustomerIdIntegrationEvent>
{
    private readonly DigiTekShopIdentityDbContext _idb;
    private readonly ILogger<CustomerCreatedHandler> _log;

    public CustomerCreatedHandler(DigiTekShopIdentityDbContext idb, ILogger<CustomerCreatedHandler> log)
    { _idb = idb; _log = log; }

    public async Task HandleAsync(AddCustomerIdIntegrationEvent e, CancellationToken ct)
    {
        var user = await _idb.Users.FirstOrDefaultAsync(u => u.Id == e.UserId, ct);
        if (user is null)
        {
            _log.LogWarning("User {UserId} not found to link CustomerId {CustomerId}", e.UserId, e.CustomerId);
            return;
        }

        if (user.CustomerId == e.CustomerId) return; 

        user.SetCustomerId(e.CustomerId);           
        await _idb.SaveChangesAsync(ct);

        _log.LogInformation("Linked User {UserId} -> Customer {CustomerId}", e.UserId, e.CustomerId);
    }
}
