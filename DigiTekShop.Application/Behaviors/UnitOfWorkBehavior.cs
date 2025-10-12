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
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!IsCommand())
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        _logger.LogDebug("Starting transaction for command: {CommandName}", requestName);

        await _uow.BeginTransactionAsync(ct);
        
        try
        {
            var response = await next();
            
            await _uow.SaveChangesAsync(ct);
            
            await _uow.DispatchDomainEventsAsync(ct);
           
            await _uow.CommitTransactionAsync(ct);
            
            _logger.LogDebug("Transaction completed successfully for: {CommandName}", requestName);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {CommandName}, rolling back", requestName);
            
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private static bool IsCommand()
    {
        return typeof(TRequest).IsAssignableTo(typeof(ICommand)) ||
               (typeof(TRequest).IsGenericType &&
                typeof(TRequest).GetGenericTypeDefinition() == typeof(ICommand<>)) ||
               typeof(TRequest).GetInterfaces()
                   .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }
}