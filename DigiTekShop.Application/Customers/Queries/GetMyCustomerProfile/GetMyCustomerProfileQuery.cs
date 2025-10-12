using DigiTekShop.Contracts.DTOs.Customer;
using MediatR;

namespace DigiTekShop.Application.Customers.Queries.GetMyCustomerProfile;

public sealed record GetMyCustomerProfileQuery(Guid UserId) : IRequest<CustomerView?>;