using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Contracts.CQRS.Commands;
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
public interface ICommand : IRequest<Result<Unit>> { }