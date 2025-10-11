using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.SetDefaultAddress;

public sealed record SetDefaultAddressCommand(Guid CustomerId, int Index)
    : IRequest<Result>;