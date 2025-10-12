using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Ef;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Persistence.Repositories.Customer
{
    public sealed class CustomerCommandRepository
        : EfCommandRepository<Domain.Customer.Entities.Customer, CustomerId>, ICustomerCommandRepository
    {
        public CustomerCommandRepository(DigiTekShopDbContext ctx) : base(ctx) { }
    }
}
