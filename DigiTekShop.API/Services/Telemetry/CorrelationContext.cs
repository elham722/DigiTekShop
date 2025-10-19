using System.Diagnostics;
using DigiTekShop.API.Common.Http;
using DigiTekShop.Contracts.Abstractions.Telemetry;

namespace DigiTekShop.API.Services.Telemetry;

public sealed class CorrelationContext(IHttpContextAccessor http) : ICorrelationContext
{
    public string? GetCorrelationId()
    {
        var hc = http.HttpContext;
        if (hc is null) return Activity.Current?.TraceId.ToString();
        if (hc.Items.TryGetValue(HeaderNames.CorrelationId, out var v) && v is string s && !string.IsNullOrWhiteSpace(s))
            return s;
        return !string.IsNullOrWhiteSpace(hc.TraceIdentifier)
            ? hc.TraceIdentifier
            : Activity.Current?.TraceId.ToString();
    }

    public string? GetCausationId() => Activity.Current?.SpanId.ToString();
}