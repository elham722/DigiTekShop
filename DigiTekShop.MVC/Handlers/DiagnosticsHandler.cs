using System.Diagnostics;

namespace DigiTekShop.MVC.Handlers;

internal sealed class DiagnosticsHandler : DelegatingHandler
{
    private readonly ILogger<DiagnosticsHandler> _logger;
    public DiagnosticsHandler(ILogger<DiagnosticsHandler> logger) => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var sw = ValueStopwatch.StartNew();
        HttpResponseMessage? resp = null;
        try
        {
            resp = await base.SendAsync(request, ct);
            return resp;
        }
        catch (TaskCanceledException tce)
        {
            _logger.LogWarning(tce, "API {Method} {Path} cancelled/timed-out after {Elapsed}ms",
                request.Method.Method, request.RequestUri?.PathAndQuery, sw.GetElapsedTime().TotalMilliseconds.ToString("F0"));
            throw;
        }
        catch (HttpRequestException hre)
        {
            _logger.LogWarning(hre, "API {Method} {Path} http error after {Elapsed}ms",
                request.Method.Method, request.RequestUri?.PathAndQuery, sw.GetElapsedTime().TotalMilliseconds.ToString("F0"));
            throw;
        }
        finally
        {
            _logger.LogInformation("API {Method} {Path} -> {Status} in {Elapsed}ms",
                request.Method.Method,
                request.RequestUri?.PathAndQuery,
                (int)(resp?.StatusCode ?? 0),
                sw.GetElapsedTime().TotalMilliseconds.ToString("F0"));
        }
    }

}

internal struct ValueStopwatch
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    private readonly long _start;
    private ValueStopwatch(long start) => _start = start;
    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());
    public TimeSpan GetElapsedTime() => new((long)(TimestampToTicks * (Stopwatch.GetTimestamp() - _start)));
}