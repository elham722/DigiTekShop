using DigiTekShop.Contracts.Repositories.Abstractions;
using MediatR;

namespace DigiTekShop.Infrastructure.Pipeline;

public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IUnitOfWork _uow;
    public UnitOfWorkBehavior(IUnitOfWork uow) => _uow = uow;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var response = await next();                // Handler اجرا می‌شود (فقط دامین/ریپو)
            await _uow.SaveChangesAsync(ct);            // یک‌بار ذخیره
            await _uow.DispatchDomainEventsAsync(ct);    // انتشار تمام DomainEventها
            await _uow.CommitTransactionAsync(ct);       // کمیت
            return response;
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}