using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Contracts.CQRS.Queries.Common;
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
