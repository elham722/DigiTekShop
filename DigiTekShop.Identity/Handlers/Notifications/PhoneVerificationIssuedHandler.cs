using Microsoft.Extensions.Hosting;

namespace DigiTekShop.Identity.Handlers.Notifications;

public sealed class PhoneVerificationIssuedHandler
    : IIntegrationEventHandler<PhoneVerificationIssuedIntegrationEvent>
{
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IPhoneSender _sms;
    private readonly IEncryptionService _crypto;
    private readonly PhoneVerificationOptions _opts;
    private readonly ILogger<PhoneVerificationIssuedHandler> _log;
    private readonly IHostEnvironment _env;

    public PhoneVerificationIssuedHandler(
        DigiTekShopIdentityDbContext db,
        IPhoneSender sms,
        IEncryptionService crypto,
        IOptions<PhoneVerificationOptions> opts,
        IHostEnvironment env,
        ILogger<PhoneVerificationIssuedHandler> log)
    {
        _db = db; _sms = sms; _crypto = crypto; _opts = opts.Value; _env = env; _log = log;
    }

    public async Task HandleAsync(PhoneVerificationIssuedIntegrationEvent e, CancellationToken ct)
    {
        var pv = await _db.PhoneVerifications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == e.PhoneVerificationId, ct);

        if (pv is null || pv.IsVerified || pv.ExpiresAtUtc <= DateTime.UtcNow)
        {
            _log.LogWarning("[OTP] PV invalid. Id={Id}", e.PhoneVerificationId);
            return;
        }

        // کد را از ProtectedString بخوان
        if (string.IsNullOrWhiteSpace(pv.EncryptedCodeProtected))
        {
            _log.LogWarning("[OTP] Missing EncryptedCode for PV={PV}", pv.Id);
            return;
        }

        var code = _crypto.Decrypt(pv.EncryptedCodeProtected, DigiTekShop.SharedKernel.Enums.Security.CryptoPurpose.TotpSecret);

        // لاگ کد فقط در Development
        if (_env.IsDevelopment())
        {
            _log.LogInformation("[OTP] Code: {Code} for phone={Phone}, PV={PV}", code, e.PhoneNumber, e.PhoneVerificationId);
        }

        // متن SMS
        var templateName = _opts.Template.OtpTemplateName;
        var res = await _sms.SendCodeAsync(e.PhoneNumber, code, templateName, ct);

        if (res.IsFailure)
        {
            _log.LogError("[OTP] Send failed to {Phone}. Err={Err}", e.PhoneNumber, res.GetFirstError());
            // OutboxWorker شما Retry را مدیریت می‌کند
            return;
        }

        _log.LogInformation("[OTP] Sent to {Phone}. PV={PV}", e.PhoneNumber, e.PhoneVerificationId);
    }
}
