using FluentValidation;
using MediatR;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count == 0) return await next();

        var messages = failures.Select(f =>
            string.IsNullOrWhiteSpace(f.PropertyName)
                ? f.ErrorMessage
                : $"{f.PropertyName}: {f.ErrorMessage}").ToList();

        const string code = "VALIDATION_FAILED";

        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result<>).MakeGenericType(valueType)
                .GetMethod("Failure", new[] { typeof(IEnumerable<string>), typeof(string) });
            
            var failure = failureMethod!.Invoke(null, new object[] { messages, code })!;
            return (TResponse)failure;
        }

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(messages, code);

        throw new ValidationException(failures);
    }
}
