// DigiTekShop.Contracts/Repositories/Customers/ICustomerCommandRepository.cs
using DigiTekShop.Contracts.Repositories.Command;
using DigiTekShop.Domain.Customer.Entities;

namespace DigiTekShop.Contracts.Repositories.Customers;

public interface ICustomerCommandRepository : ICommandRepository<Domain.Customer.Entities.Customer, CustomerId>
{
    // فعلاً چیزی اضافه نیاز نیست؛ اگر عملیات خاص کامندی خواستی اینجا اضافه کن
}