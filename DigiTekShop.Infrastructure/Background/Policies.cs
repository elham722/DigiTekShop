using Polly;
using Polly.Retry;

namespace DigiTekShop.Infrastructure.Background;

internal static class Policies
{
    public static AsyncRetryPolicy RetryBus() =>
        Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: i => TimeSpan.FromMilliseconds(200 * Math.Pow(2, i)), // 200, 400, 800
                onRetry: (ex, delay, attempt, ctx) =>
                {
                    // می‌تونی اینجا لاگ بزنی
                });
}