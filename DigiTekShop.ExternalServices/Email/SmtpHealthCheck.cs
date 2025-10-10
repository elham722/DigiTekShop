using DigiTekShop.Contracts.DTOs.Auth.EmailSender;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using DigiTekShop.Contracts.DTOs.Auth.EmailSender;
namespace DigiTekShop.ExternalServices.Email
{
    public sealed class SmtpHealthCheck : IHealthCheck
    {
        private readonly IOptions<SmtpSettings> _opt;
        public SmtpHealthCheck(IOptions<SmtpSettings> opt) => _opt = opt;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            var s = _opt.Value;
            if (string.IsNullOrWhiteSpace(s.Host) || s.Port <= 0)
                return Task.FromResult(HealthCheckResult.Unhealthy("SMTP not configured"));

            // این چک فقط کانفیگ را بررسی می‌کند؛ اگر اتصال واقعی می‌خواهی، با timeout کوتاه ارسال تست بزن (هزینه‌دار است).
            return Task.FromResult(HealthCheckResult.Healthy("SMTP configured"));
        }
    }

}
