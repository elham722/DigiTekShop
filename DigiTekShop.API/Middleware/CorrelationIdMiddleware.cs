using Serilog.Context;

namespace DigiTekShop.API.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        private const string DefaultHeaderName = "X-Request-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly string _headerName;

        // ← پارامتر اختیاری headerName اضافه شد
        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger,
            string? headerName = null)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _headerName = string.IsNullOrWhiteSpace(headerName) ? DefaultHeaderName : headerName.Trim();
        }

        public async Task Invoke(HttpContext context)
        {
            var incoming = context.Request.Headers[_headerName].FirstOrDefault();
            var correlationId = string.IsNullOrWhiteSpace(incoming) ? CreateId() : incoming.Trim();

            context.TraceIdentifier = correlationId;

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(_headerName))
                    context.Response.Headers.Add(_headerName, correlationId);
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (_logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
            {
                await _next(context);
            }
        }

        private static string CreateId()
            => $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
    }

    public static class CorrelationIdExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
            => app.UseMiddleware<CorrelationIdMiddleware>();

        // ← اورلود جدید با پارامتر هدر
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app, string headerName)
            => app.UseMiddleware<CorrelationIdMiddleware>(headerName);
    }
}
