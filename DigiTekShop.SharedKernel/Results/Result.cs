using System.Collections.Immutable;
using System.Diagnostics;

namespace DigiTekShop.SharedKernel.Results;

[DebuggerDisplay("IsSuccess = {IsSuccess}, Errors = {Errors.Count}")]
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }
    public string? ErrorCode { get; }
    public DateTimeOffset Timestamp { get; }

    protected Result(bool isSuccess, IEnumerable<string>? errors, string? errorCode = null)
    {
        IsSuccess = isSuccess;

        var list = (errors ?? Array.Empty<string>()).Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();

        if (!isSuccess && list.Length == 0)
            throw new ArgumentException("Failure result must contain at least one error.", nameof(errors));

        Errors = list.ToImmutableArray();
        ErrorCode = errorCode;
        Timestamp = DateTimeOffset.UtcNow;
    }

    public void Deconstruct(out bool isSuccess, out IReadOnlyList<string> errors)
    { isSuccess = IsSuccess; errors = Errors; }

    #region Static Factories
    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(string error, string? errorCode = null) => new(false, new[] { error }, errorCode);
    public static Result Failure(IEnumerable<string> errors, string? errorCode = null) => new(false, errors, errorCode);

    public static Result FromException(Exception ex, string? errorCode = null)
        => Failure(ex.Message, errorCode ?? ex.GetType().Name);

    public static Result From(bool ok, string? errorIfFalse = null, string? errorCode = null)
        => ok ? Success() : Failure(errorIfFalse ?? "Operation failed.", errorCode);
    #endregion


    #region Implicit Conversions
    public static implicit operator Result(string error) => Failure(error);
    public static implicit operator Result(List<string> errors) => Failure(errors);
    #endregion

    #region Functional Helpers
    public Result<TOut> Map<TOut>(Func<TOut> mapper)
        => IsSuccess ? Result<TOut>.Success(mapper()) : Result<TOut>.Failure(Errors, ErrorCode);

    public Result Bind(Func<Result> binder)
        => IsSuccess ? binder() : this;

    public Result OnSuccess(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    public Result OnFailure(Action<IReadOnlyList<string>> action)
    {
        if (IsFailure) action(Errors);
        return this;
    }
    #endregion

    #region Matching
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Errors);

    public void Match(Action onSuccess, Action<IReadOnlyList<string>> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Errors);
    }
    #endregion

    #region Safe Accessors
    public string? GetFirstError() => Errors.FirstOrDefault();
    public string GetErrorsAsString(string separator = "; ") => string.Join(separator, Errors);
    public bool HasErrorCode(string errorCode) => ErrorCode == errorCode;
    #endregion

    #region Overrides
    public override string ToString() => IsSuccess ? "Success" : $"Failure: {GetErrorsAsString()}";

    public override bool Equals(object? obj)
    {
        if (obj is not Result other) return false;
        return IsSuccess == other.IsSuccess &&
               Errors.SequenceEqual(other.Errors) &&
               ErrorCode == other.ErrorCode;
    }

    public override int GetHashCode() => HashCode.Combine(IsSuccess, Errors, ErrorCode);
    #endregion
}
