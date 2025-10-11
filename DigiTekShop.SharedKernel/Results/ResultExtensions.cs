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

    public static Result ToResult<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
        => result.IsFailure ? Result.Failure(result.Errors, result.ErrorCode)
            : predicate(result.Value!) ? Result.Success()
            : Result.Failure(errorMessage);

    public static Result WithCode(this Result result, string errorCode)
    {
        if (result.IsSuccess) return result; 
       
        return Result.Failure(result.Errors, errorCode);
    }

    public static Result<T> WithCode<T>(this Result<T> result, string errorCode)
    {
        if (result.IsSuccess) return result;
        return Result<T>.Failure(result.Errors, errorCode);
    }

    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> r, Func<TIn, TOut> f)
        => r.IsSuccess ? Result<TOut>.Success(f(r.Value!)) : Result<TOut>.Failure(r.Errors, r.ErrorCode);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> r, Func<TIn, Task<Result<TOut>>> f)
        => r.IsSuccess ? await f(r.Value!) : Result<TOut>.Failure(r.Errors, r.ErrorCode);

    public static Result Ensure(this Result r, Func<bool> predicate, string error, string? code = null)
        => r.IsSuccess && !predicate() ? Result.Failure(error, code) : r;

}