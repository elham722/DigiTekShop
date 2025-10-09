
using DigiTekShop.SharedKernel.Errors;

namespace DigiTekShop.SharedKernel.Results;

public static class ResultFactories
{
    public static Result Fail(string code, string? message = null)
        => Result.Failure(message ?? ErrorCatalog.Resolve(code).DefaultMessage, code);

    public static Result<T> Fail<T>(string code, string? message = null)
        => Result<T>.Failure(message ?? ErrorCatalog.Resolve(code).DefaultMessage, code);
}