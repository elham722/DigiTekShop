using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.Domain.Customer.ValueObjects;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Ef;

namespace DigiTekShop.Persistence.Repositories.Customer;

public sealed class CustomerCommandRepository
    : EfCommandRepository<Domain.Customer.Entities.Customer, CustomerId>, 
      ICustomerCommandRepository
{
    public CustomerCommandRepository(DigiTekShopDbContext ctx) : base(ctx)
    {
    }

}