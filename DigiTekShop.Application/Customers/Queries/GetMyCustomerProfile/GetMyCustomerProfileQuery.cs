using DigiTekShop.Contracts.DTOs.Customer;

namespace DigiTekShop.Application.Customers.Queries.GetMyCustomerProfile;

public sealed record GetMyCustomerProfileQuery(Guid UserId) : IQuery<CustomerView?>;