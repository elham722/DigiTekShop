namespace DigiTekShop.Contracts.CQRS.Queries;
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }