using System.Collections.Immutable;

namespace DigiTekShop.SharedKernel.Results;

[DebuggerDisplay("IsSuccess = {IsSuccess}, Errors = {Errors.Count}, Code = {ErrorCode}")]
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }
    public string? ErrorCode { get; }
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; }

    protected Result(
        bool isSuccess,
        IEnumerable<string>? errors,
        string? errorCode = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        IsSuccess = isSuccess;

        var list = (errors ?? Array.Empty<string>())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToArray();

        if (!isSuccess && list.Length == 0)
            throw new ArgumentException("Failure result must contain at least one error.", nameof(errors));

        Errors = list.ToImmutableArray();
        ErrorCode = errorCode;
        Timestamp = DateTimeOffset.UtcNow;
        Metadata = metadata;
    }

    public void Deconstruct(out bool ok, out IReadOnlyList<string> errors)
    { ok = IsSuccess; errors = Errors; }

    public static Result Success() => new(true, Array.Empty<string>(), null, null);
    public static Result Failure(string error, string? errorCode = null)
        => new(false, new[] { error }, errorCode, null);
    public static Result Failure(IEnumerable<string> errors, string? errorCode = null)
        => new(false, errors, errorCode, null);

    public static Result FromException(Exception ex, string? errorCode = null)
        => Failure(ex.Message, errorCode ?? ex.GetType().Name);

    public static Result From(bool ok, string? errorIfFalse = null, string? errorCode = null)
        => ok ? Success() : Failure(errorIfFalse ?? "Operation failed.", errorCode);

    public static implicit operator Result(string error) => Failure(error);
    public static implicit operator Result(List<string> errors) => Failure(errors);

    public Result WithMeta(string key, object? value)
    {
        var dict = (Metadata as ImmutableDictionary<string, object?>)
                   ?? ImmutableDictionary<string, object?>.Empty;

        var updated = value is null ? dict.Remove(key) : dict.SetItem(key, value);
        return new Result(IsSuccess, Errors, ErrorCode, updated);
    }

    public bool TryGetMeta<T>(string key, out T? value)
    {
        value = default;
        if (Metadata is null) return false;
        if (Metadata.TryGetValue(key, out var obj) && obj is T t)
        {
            value = t;
            return true;
        }
        return false;
    }

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

    
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Errors);

    public void Match(Action onSuccess, Action<IReadOnlyList<string>> onFailure)
    {
        if (IsSuccess) onSuccess();
        else onFailure(Errors);
    }

   
    public string? GetFirstError() => Errors.FirstOrDefault();
    public string GetErrorsAsString(string sep = "; ") => string.Join(sep, Errors);
    public bool HasErrorCode(string code) => ErrorCode == code;

    public override string ToString() => IsSuccess ? "Success" : $"Failure[{ErrorCode ?? "-"}]: {GetErrorsAsString()}";

    public override bool Equals(object? obj)
    {
        if (obj is not Result other) return false;
        return IsSuccess == other.IsSuccess
            && ErrorCode == other.ErrorCode
            && Errors.SequenceEqual(other.Errors);
    }

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(IsSuccess);
        hc.Add(ErrorCode);
        foreach (var e in Errors) hc.Add(e);
        return hc.ToHashCode();
    }
}
