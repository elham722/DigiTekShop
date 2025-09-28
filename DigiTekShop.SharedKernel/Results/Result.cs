namespace DigiTekShop.SharedKernel.Results;

    public class Result
    {
        public bool IsSuccess { get; }
        public string? Error { get; }

        protected Result(bool success, string? error)
        {
            IsSuccess = success;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Fail(string error) => new Result(false, error);
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        protected Result(T value) : base(true, null) => Value = value;
        protected Result(string error) : base(false, error) => Value = default;

        public static Result<T> Success(T value) => new Result<T>(value);
        public static new Result<T> Fail(string error) => new Result<T>(error);
    }
