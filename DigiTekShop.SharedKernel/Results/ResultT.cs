using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DigiTekShop.SharedKernel.Results;

[DebuggerDisplay("IsSuccess = {IsSuccess}, Value = {Value}, Errors = {Errors.Count}")]
public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, Array.Empty<string>()) => Value = value;

    private Result(IEnumerable<string> errors, string? errorCode = null)
        : base(false, errors, errorCode) => Value = default;

    #region Static Factories
    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(string error) => new(new[] { error });
    public static new Result<T> Failure(IEnumerable<string> errors) => new(errors);
    public static new Result<T> Failure(string error, string errorCode) => new(new[] { error }, errorCode);
    public static new Result<T> Failure(IEnumerable<string> errors, string errorCode) => new(errors, errorCode);
    public static Result<T> FromException(Exception ex, string? errorCode = null)
        => Failure(ex.Message, errorCode ?? ex.GetType().Name);
    #endregion

    #region Implicit Conversions
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(string error) => Failure(error);
    public static implicit operator Result<T>(List<string> errors) => Failure(errors);
    #endregion

    #region Functional Helpers
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Success(mapper(Value!)) : Result<TOut>.Failure(Errors, ErrorCode);
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        => IsSuccess ? binder(Value!) : Result<TOut>.Failure(Errors, ErrorCode);

    public Result Bind(Func<T, Result> binder)
        => IsSuccess ? binder(Value!) : this;

    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    public new Result<T> OnFailure(Action<IReadOnlyList<string>> action)
    {
        if (IsFailure) action(Errors);
        return this;
    }

    public Result<T> Filter(Func<T, bool> predicate, string errorMessage)
        => IsFailure ? this : (predicate(Value!) ? this : Failure(errorMessage));

    public Result<T> Filter(Func<T, bool> predicate, string errorMessage, string errorCode)
        => IsFailure ? this : (predicate(Value!) ? this : Failure(errorMessage, errorCode));
    #endregion

    #region Matching
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Errors);

    public void Match(Action<T> onSuccess, Action<IReadOnlyList<string>> onFailure)
    {
        if (IsSuccess) onSuccess(Value!);
        else onFailure(Errors);
    }
    #endregion

    #region Safe Accessors
    public T GetValueOrThrow()
    {
        if (IsFailure)
            throw new InvalidOperationException($"Cannot get value from failed result. Errors: {GetErrorsAsString()}");
        return Value!;
    }

    public T GetValueOrDefault(T defaultValue = default!) => IsSuccess ? Value! : defaultValue;

    public T GetValueOrElse(Func<T> defaultValueFactory) => IsSuccess ? Value! : defaultValueFactory();

    public T GetValueOrElse(Func<IReadOnlyList<string>, T> defaultValueFactory)
        => IsSuccess ? Value! : defaultValueFactory(Errors);
    #endregion

    #region Async Helpers
    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper)
        => IsSuccess ? Result<TOut>.Success(await mapper(Value!)) : Result<TOut>.Failure(Errors, ErrorCode);

    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder)
        => IsSuccess ? await binder(Value!) : Result<TOut>.Failure(Errors, ErrorCode);

    public async Task<Result<T>> OnSuccessAsync(Func<T, Task> action)
    {
        if (IsSuccess) await action(Value!);
        return this;
    }
    #endregion
}
