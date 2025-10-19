using DigiTekShop.Contracts.Abstractions.CQRS.Commands;
using DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Application.Behaviors;

public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UnitOfWorkBehavior<TRequest, TResponse>> _logger;

    public UnitOfWorkBehavior(IUnitOfWork uow, ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger)
        => (_uow, _logger) = (uow, logger);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // فقط روی Commandها
        if (!IsCommand<TRequest>())
            return await next();

        // اگر NonTransactional بود (فقط Identity)، بدون UoW رد شو
        if (IsNonTransactional(request))
            return await next();

        // transactional-command:
        var response = await next();

        // تغییرات AppDb + Outbox
        await _uow.SaveChangesAsync(ct);

        return response;
    }

    static bool IsCommand<T>() =>
        typeof(T).IsAssignableTo(typeof(ICommand)) ||
        typeof(T).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

    static bool IsNonTransactional(TRequest req) =>
        req is INonTransactionalCommand;
}