using MediatR;

namespace DigiTekShop.Contracts.CQRS.Commands.Common;
public interface ICommand<out TResponse> : IRequest<TResponse> { }

public interface ICommand : ICommand<Unit> { }