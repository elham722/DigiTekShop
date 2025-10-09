using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Contracts.CQRS.Queries;
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }