using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.SharedKernel.Results;
public static class ResultExtensions
{
    // Async unwrap عمومی
    public static async Task<Result<T>> UnwrapAsync<T>(this Task<Result<T>> task) => await task;
    public static async Task<Result> UnwrapAsync(this Task<Result> task) => await task;

    // Combine عمومی
    public static Result Combine(this IEnumerable<Result> results)
    {
        var errors = results.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToList();
        return errors.Any() ? Result.Failure(errors) : Result.Success();
    }

    public static Result<IEnumerable<T>> Combine<T>(this IEnumerable<Result<T>> results)
    {
        var errors = results.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToList();
        if (errors.Any()) return Result<IEnumerable<T>>.Failure(errors);
        var values = results.Where(r => r.IsSuccess).Select(r => r.Value);
        return Result<IEnumerable<T>>.Success(values);
    }

    // Nullable و predicate
    public static Result ToResult<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
        => result.IsSuccess && predicate(result.Value) ? Result.Success() : Result.Failure(errorMessage);

    public static Result<T> ToResult<T>(this T? value, string errorMessage = "Value is null")
        => value is not null ? Result<T>.Success(value) : Result<T>.Failure(errorMessage);

    public static Result<T> ToResult<T>(this T? value, string errorMessage, string errorCode)
        => value is not null ? Result<T>.Success(value) : Result<T>.Failure(errorMessage, errorCode);
}