using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.MVC.Services;

public readonly record struct ApiResult<T>(
    bool Success,
    T? Data,
    ProblemDetails? Problem,
    HttpStatusCode StatusCode,
    HttpResponseHeaders? Headers = null)
{
    public static ApiResult<T> Ok(T? data, HttpStatusCode code = HttpStatusCode.OK, HttpResponseHeaders? headers = null)
        => new(true, data, null, code, headers);

    public static ApiResult<T> Fail(ProblemDetails? problem, HttpStatusCode code, HttpResponseHeaders? headers = null)
        => new(false, default, problem, code, headers);
}

public readonly record struct Unit
{
    public static readonly Unit Value = new();
}

public sealed record FormFilePart(string Name, string FileName, Stream Content, string? ContentType = null);

internal sealed record ApiEnvelope<T>(T? Data, string? TraceId, DateTimeOffset? Timestamp);