using DigiTekShop.Contracts.DTOs.Mfa;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;

namespace DigiTekShop.Identity.Services
{
    public class MfaService
    {
        private readonly DigiTekShopIdentityDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IDataProtector _protector;
        private readonly ILogger<MfaService> _logger;

        private const int MaxAttempts = 5;

        public MfaService(
            DigiTekShopIdentityDbContext context,
            UserManager<User> userManager,
            IDataProtectionProvider protectionProvider,
            ILogger<MfaService> logger)
        {
            _context = context;
            _userManager = userManager;
            _protector = protectionProvider.CreateProtector("MfaSecretKey");
            _logger = logger;
        }

        public async Task<MfaSetupDto> SetupAsync(User user)
        {
            Guard.AgainstNull(user, nameof(user));

            var secretKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
            var encryptedKey = _protector.Protect(secretKey);

            var otpauthUrl = $"otpauth://totp/DigiTekShop:{user.Email}?secret={secretKey}&issuer=DigiTekShop";

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBase64 = Convert.ToBase64String(qrCode.GetGraphic(20));

            var existing = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (existing != null)
            {
                existing.Enable(encryptedKey);   // ✅ اگر وجود داشت آپدیت کن
            }
            else
            {
                _context.UserMfa.Add(UserMfa.Create(user.Id, encryptedKey));
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("MFA setup enabled for user {UserId}", user.Id);

            return new MfaSetupDto(qrBase64, secretKey);
        }

        public async Task<Result> ValidateCodeAsync(User user, string code)
        {
            var record = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (record == null) return Result.Failure("MFA not configured.");

            if (!record.IsEnabled) return Result.Failure("MFA is disabled.");

            if (record.Attempts >= MaxAttempts)
            {
                _logger.LogWarning("MFA max attempts exceeded for user {UserId}", user.Id);
                return Result.Failure("Too many attempts.");
            }

            record.IncrementAttempts(MaxAttempts);

            var secretKey = _protector.Unprotect(record.SecretKeyEncrypted);
            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            var isValid = totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));

            if (!isValid)
            {
                await _context.SaveChangesAsync();
                _logger.LogWarning("Invalid MFA code for user {UserId}", user.Id);
                return Result.Failure("Invalid code.");
            }

            record.MarkVerified();  // ✅ ثبت زمان آخرین موفقیت
            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA validated successfully for user {UserId}", user.Id);
            return Result.Success();
        }

        public async Task<Result> DisableAsync(User user)
        {
            var record = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (record == null) return Result.Failure("MFA not enabled.");

            record.Disable();
            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA disabled for user {UserId}", user.Id);
            return Result.Success();
        }

        public async Task<MfaStatusDto> GetStatusAsync(User user)
        {
            var record = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserId == user.Id);
            return new MfaStatusDto(record?.IsEnabled ?? false);
        }
    }

}
