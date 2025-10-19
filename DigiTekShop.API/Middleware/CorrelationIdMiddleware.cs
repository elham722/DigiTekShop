using System.Diagnostics;
using DigiTekShop.API.Common.Http;
using Serilog.Context;

namespace DigiTekShop.API.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerName;

    public CorrelationIdMiddleware(RequestDelegate next, string? headerName = null)
    {
        _next = next;
        _headerName = headerName ?? HeaderNames.CorrelationId;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers[_headerName].FirstOrDefault()
            ?? context.TraceIdentifier;

        context.Items[_headerName] = correlationId;

        using var act = new Activity("HTTP Request");
        act.SetIdFormat(ActivityIdFormat.W3C);
        act.Start();

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(_headerName))
                context.Response.Headers[_headerName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

}

