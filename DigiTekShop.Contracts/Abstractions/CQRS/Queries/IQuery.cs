namespace DigiTekShop.Contracts.Abstractions.CQRS.Queries;
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }