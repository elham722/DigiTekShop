using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.SharedKernel.Results;
public static class ResultExtensions
{
    // Async unwrap عمومی
    public static async Task<Result<T>> UnwrapAsync<T>(this Task<Result<T>> task) => await task;
    public static async Task<Result> UnwrapAsync(this Task<Result> task) => await task;

    public static Result Combine(this IEnumerable<Result> results, string? errorCode = null)
    {
        var errs = results.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToArray();
        return errs.Length > 0 ? Result.Failure(errs, errorCode ?? "COMBINED_ERROR") : Result.Success();
    }

    public static Result<IEnumerable<T>> Combine<T>(this IEnumerable<Result<T>> results, string? errorCode = null)
    {
        var arr = results as Result<T>[] ?? results.ToArray();
        var errs = arr.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToArray();
        if (errs.Length > 0) return Result<IEnumerable<T>>.Failure(errs, errorCode ?? "COMBINED_ERROR");
        var vals = arr.Where(r => r.IsSuccess).Select(r => r.Value!);
        return Result<IEnumerable<T>>.Success(vals);
    }

    // Nullable و predicate
    public static Result ToResult<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
        => result.IsSuccess && predicate(result.Value) ? Result.Success() : Result.Failure(errorMessage);

    public static Result<T> ToResult<T>(this T? value, string errorMessage = "Value is null")
        => value is not null ? Result<T>.Success(value) : Result<T>.Failure(errorMessage);

    public static Result<T> ToResult<T>(this T? value, string errorMessage, string errorCode)
        => value is not null ? Result<T>.Success(value) : Result<T>.Failure(errorMessage, errorCode);
}