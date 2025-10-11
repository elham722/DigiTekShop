using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.ChangeEmail;

public sealed record ChangeEmailCommand(Guid CustomerId, string NewEmail)
    : IRequest<Result>;