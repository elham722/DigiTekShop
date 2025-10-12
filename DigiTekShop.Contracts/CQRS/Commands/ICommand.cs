namespace DigiTekShop.Contracts.CQRS.Commands;
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
public interface ICommand : IRequest<Result> { }