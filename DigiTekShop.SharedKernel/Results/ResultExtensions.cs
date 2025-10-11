namespace DigiTekShop.SharedKernel.Results;

public static class ResultExtensions
{
    public static Result Combine(this IEnumerable<Result> results, string? fallbackCode = "COMBINED_ERROR")
    {
        var arr = results as Result[] ?? results.ToArray();
        var errs = arr.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToArray();
        if (errs.Length == 0) return Result.Success();

        var code = arr.Select(r => r.ErrorCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? fallbackCode;
        return Result.Failure(errs, code);
    }

    public static Result<IEnumerable<T>> Combine<T>(this IEnumerable<Result<T>> results, string? fallbackCode = "COMBINED_ERROR")
    {
        var arr = results as Result<T>[] ?? results.ToArray();
        var errs = arr.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToArray();
        if (errs.Length > 0)
        {
            var code = arr.Select(r => r.ErrorCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? fallbackCode;
            return Result<IEnumerable<T>>.Failure(errs, code);
        }

        var vals = arr.Where(r => r.IsSuccess).Select(r => r.Value!);
        return Result<IEnumerable<T>>.Success(vals);
    }

    public static Result ToResult<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
        => result.IsFailure ? Result.Failure(result.Errors, result.ErrorCode)
            : predicate(result.Value!) ? Result.Success()
            : Result.Failure(errorMessage);

    public static Result WithCode(this Result r, string errorCode)
        => r.IsSuccess ? r : Result.Failure(r.Errors, errorCode);

    public static Result<T> WithCode<T>(this Result<T> r, string errorCode)
        => r.IsSuccess ? r : Result<T>.Failure(r.Errors, errorCode);

    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> r, Func<TIn, TOut> f)
        => r.IsSuccess ? Result<TOut>.Success(f(r.Value!)) : Result<TOut>.Failure(r.Errors, r.ErrorCode);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> r, Func<TIn, Task<Result<TOut>>> f)
        => r.IsSuccess ? await f(r.Value!) : Result<TOut>.Failure(r.Errors, r.ErrorCode);

    public static Result Ensure(this Result r, Func<bool> predicate, string error, string? code = null)
        => r.IsSuccess && !predicate() ? Result.Failure(error, code) : r;
}
