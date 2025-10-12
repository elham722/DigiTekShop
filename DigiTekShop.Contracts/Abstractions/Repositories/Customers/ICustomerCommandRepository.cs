using DigiTekShop.Contracts.Abstractions.Repositories.Common.Command;

namespace DigiTekShop.Contracts.Abstractions.Repositories.Customers;

public interface ICustomerCommandRepository : ICommandRepository<Customer, CustomerId>
{
  
}