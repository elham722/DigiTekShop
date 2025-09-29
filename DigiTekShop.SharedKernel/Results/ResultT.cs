namespace DigiTekShop.SharedKernel.Results;

[DebuggerDisplay("IsSuccess = {IsSuccess}, Value = {Value}, Errors = {Errors.Count}")]
public class Result<T> : Result
{
    public T Value { get; }

    private Result(T value) : base(true, Array.Empty<string>())
    {
        Value = value;
    }

    private Result(IEnumerable<string> errors, string? errorCode = null) : base(false, errors, errorCode)
    {
        Value = default!;
    }

    #region Static Factories

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(string error) => new(new[] { error });

    public static new Result<T> Failure(IEnumerable<string> errors) => new(errors);

    public static new Result<T> Failure(string error, string errorCode) => new(new[] { error }, errorCode);

    public static new Result<T> Failure(IEnumerable<string> errors, string errorCode) => new(errors, errorCode);

    #endregion

    #region Implicit Conversions

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(string error) => Failure(error);
    public static implicit operator Result<T>(List<string> errors) => Failure(errors);

    #endregion

    #region Functional Helpers

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Success(mapper(Value)) : Result<TOut>.Failure(Errors, ErrorCode);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        => IsSuccess ? binder(Value) : Result<TOut>.Failure(Errors, ErrorCode);

    public Result Bind(Func<T, Result> binder)
        => IsSuccess ? binder(Value) : this;

    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value);
        return this;
    }

    public new Result<T> OnFailure(Action<IReadOnlyList<string>> action)
    {
        if (IsFailure) action(Errors);
        return this;
    }

    public Result<T> Filter(Func<T, bool> predicate, string errorMessage)
    {
        if (IsFailure) return this;
        return predicate(Value) ? this : Failure(errorMessage);
    }

    public Result<T> Filter(Func<T, bool> predicate, string errorMessage, string errorCode)
    {
        if (IsFailure) return this;
        return predicate(Value) ? this : Failure(errorMessage, errorCode);
    }

    #endregion

    #region Matching

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Errors);

    public void Match(Action<T> onSuccess, Action<IReadOnlyList<string>> onFailure)
    {
        if (IsSuccess) onSuccess(Value);
        else onFailure(Errors);
    }

    #endregion

    #region Safe Accessors

    public T GetValueOrThrow()
    {
        if (IsFailure)
            throw new InvalidOperationException($"Cannot get value from failed result. Errors: {GetErrorsAsString()}");

        return Value;
    }

    public T GetValueOrDefault(T defaultValue = default!)
        => IsSuccess ? Value : defaultValue;

    public T GetValueOrElse(Func<T> defaultValueFactory)
        => IsSuccess ? Value : defaultValueFactory();

    public T GetValueOrElse(Func<IReadOnlyList<string>, T> defaultValueFactory)
        => IsSuccess ? Value : defaultValueFactory(Errors);

    #endregion

    #region Async Helpers

    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper)
        => IsSuccess ? Result<TOut>.Success(await mapper(Value)) : Result<TOut>.Failure(Errors, ErrorCode);

    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder)
        => IsSuccess ? await binder(Value) : Result<TOut>.Failure(Errors, ErrorCode);

    public async Task<Result<T>> OnSuccessAsync(Func<T, Task> action)
    {
        if (IsSuccess) await action(Value);
        return this;
    }

    #endregion

    #region Overrides

    public override string ToString()
        => IsSuccess ? $"Success: {Value}" : $"Failure: {GetErrorsAsString()}";

    public override bool Equals(object? obj)
    {
        if (obj is not Result<T> other) return false;
        return IsSuccess == other.IsSuccess &&
               EqualityComparer<T>.Default.Equals(Value, other.Value) &&
               Errors.SequenceEqual(other.Errors) &&
               ErrorCode == other.ErrorCode;
    }

    public override int GetHashCode()
        => HashCode.Combine(IsSuccess, Value, Errors, ErrorCode);

    #endregion
}

