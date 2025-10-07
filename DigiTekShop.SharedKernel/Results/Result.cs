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
        Errors = (errors ?? Array.Empty<string>()).ToList().AsReadOnly();
        ErrorCode = errorCode;
        Timestamp = DateTimeOffset.UtcNow;
    }

    #region Static Factories
    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(string error) => new(false, new[] { error });
    public static Result Failure(IEnumerable<string> errors) => new(false, errors);
    public static Result Failure(string error, string errorCode) => new(false, new[] { error }, errorCode);
    public static Result Failure(IEnumerable<string> errors, string errorCode) => new(false, errors, errorCode);

    // ✅ مفید وقتی استثنا را مستقیم تبدیل می‌کنی
    public static Result FromException(Exception ex, string? errorCode = null)
        => Failure(ex.Message, errorCode ?? ex.GetType().Name);
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
