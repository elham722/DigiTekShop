using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.MVC.Services;

public readonly record struct ApiResult<T>(
    bool Success,
    T? Data,
    ProblemDetails? Problem,
    HttpStatusCode StatusCode)
{
    public static ApiResult<T> Ok(T? data, HttpStatusCode code = HttpStatusCode.OK)
        => new(true, data, null, code);

    public static ApiResult<T> Fail(ProblemDetails? problem, HttpStatusCode code)
        => new(false, default, problem, code);
}

public readonly record struct Unit
{
    public static readonly Unit Value = new();
}

public sealed record FormFilePart(string Name, string FileName, Stream Content, string? ContentType = null);

internal sealed record ApiEnvelope<T>(T? Data, string? TraceId, DateTimeOffset? Timestamp);