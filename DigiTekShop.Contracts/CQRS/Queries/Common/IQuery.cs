using MediatR;

namespace DigiTekShop.Contracts.CQRS.Queries.Common;
public interface IQuery<out TResponse> : IRequest<TResponse> { }