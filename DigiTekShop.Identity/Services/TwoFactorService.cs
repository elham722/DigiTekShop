using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.Abstractions.Identity.Encryption;
using DigiTekShop.Contracts.DTOs.Auth.Mfa;
using DigiTekShop.Contracts.DTOs.Auth.TwoFactor;
using DigiTekShop.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;

namespace DigiTekShop.Identity.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly DigiTekShopIdentityDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<TwoFactorService> _logger;

        private const int MaxAttempts = 5;
        private const int DefaultWindow = 1; 
        private const int MaxDrift = 2; 

        public TwoFactorService(
            DigiTekShopIdentityDbContext context,
            UserManager<User> userManager,
            IEncryptionService encryptionService,
            ILogger<TwoFactorService> logger)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _logger = logger;
        }

     
        public async Task<MfaSetupDto> SetupAsync(User user)
        {
            Guard.AgainstNull(user, nameof(user));

            var secretKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
            var encryptedKey = _encryptionService.Encrypt(secretKey);

            var issuer = Uri.EscapeDataString("DigiTekShop");
            var label = Uri.EscapeDataString(user.Email);
            var otpauthUrl = $"otpauth://totp/{issuer}:{label}?secret={secretKey}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBase64 = Convert.ToBase64String(qrCode.GetGraphic(20));

            var existing = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (existing != null)
            {
             
                existing.Enable(encryptedKey);
            }
            else
            {
                var userMfa = UserMfa.Create(user.Id, encryptedKey);
                _context.UserMfa.Add(userMfa);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("MFA setup enabled for user {UserId}", user.Id);

            return new MfaSetupDto(qrBase64, secretKey);
        }

        public async Task<Result> ValidateCodeAsync(User user, string code, int? window = null, int? maxDrift = null)
        {
            var record = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (record == null) return Result.Failure("MFA not configured.");

            if (!record.IsEnabled) return Result.Failure("MFA is disabled.");

            
            if (record.IsCurrentlyLocked)
            {
                _logger.LogWarning("MFA is locked for user {UserId} until {LockedUntil}", user.Id, record.LockedUntil);
                return Result.Failure($"MFA is locked until {record.LockedUntil:yyyy-MM-dd HH:mm:ss} UTC");
            }

            
            if (record.IsLockExpired)
            {
                record.UnlockMfa();
            }

            try
            {
                record.IncrementAttempts(MaxAttempts, TimeSpan.FromMinutes(15));
            }
            catch (InvalidOperationException ex)
            {
                await _context.SaveChangesAsync();
                _logger.LogWarning("MFA locked for user {UserId} due to max attempts", user.Id);
                return Result.Failure(ex.Message);
            }

            var secretKey = _encryptionService.Decrypt(record.SecretKeyEncrypted);
            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            
            
            var verificationWindow = new VerificationWindow(
                window ?? DefaultWindow, 
                window ?? DefaultWindow
            );
            
            var isValid = totp.VerifyTotp(code, out var timeStepMatched, verificationWindow);

            
            if (isValid && maxDrift.HasValue)
            {
                var currentTimeStep = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds / 30;
                var drift = Math.Abs(timeStepMatched - currentTimeStep);
                
                if (drift > maxDrift.Value)
                {
                    _logger.LogWarning("MFA code drift too large for user {UserId}: {Drift} steps", user.Id, drift);
                    await _context.SaveChangesAsync();
                    return Result.Failure("Code drift too large. Please sync your device time.");
                }
            }

            if (!isValid)
            {
                await _context.SaveChangesAsync();
                _logger.LogWarning("Invalid MFA code for user {UserId}", user.Id);
                return Result.Failure("Invalid code.");
            }

            record.MarkVerified();
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
            await _context.Entry(user).Reference(u => u.Mfa).LoadAsync();
            return new MfaStatusDto(user.Mfa?.IsEnabled ?? false);
        }

       

        public async Task<Result<TwoFactorResponseDto>> EnableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default)
        {
            
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Result<TwoFactorResponseDto>.Failure("UserId is required.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null || user.IsDeleted)
                return Result<TwoFactorResponseDto>.Failure("User not found or inactive.");

            
            var setup = await SetupAsync(user);

           
            if (user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = false;
                await _userManager.UpdateAsync(user);
            }

          
            var resp = new TwoFactorResponseDto(
                Enabled: false,
              TwoFactorProvider.Sms
            );

            return Result<TwoFactorResponseDto>.Success(resp);
        }

        public async Task<Result<TwoFactorResponseDto>> DisableTwoFactorAsync(TwoFactorRequestDto request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Result<TwoFactorResponseDto>.Failure("UserId is required.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null || user.IsDeleted)
                return Result<TwoFactorResponseDto>.Failure("User not found or inactive.");

           
            var res = await DisableAsync(user);
            if (res.IsFailure) return Result<TwoFactorResponseDto>.Failure(res.Errors);

            
            if (user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = false;
                await _userManager.UpdateAsync(user);
            }

            var dto = new TwoFactorResponseDto(
                Enabled: false,
                TwoFactorProvider.Sms
            );
            return Result<TwoFactorResponseDto>.Success(dto);
        }

        public async Task<Result> VerifyTwoFactorTokenAsync(VerifyTwoFactorRequestDto request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Result.Failure("UserId is required.");
            if (string.IsNullOrWhiteSpace(request.Code))
                return Result.Failure("Code is required.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null || user.IsDeleted)
                return Result.Failure("User not found or inactive.");

            var validation = await ValidateCodeAsync(user, request.Code);
            if (validation.IsFailure) return validation;

            
            if (!user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = true;
                await _userManager.UpdateAsync(user);
            }

            return Result.Success();
        }

        public async Task<Result<TwoFactorTokenResponseDto>> GenerateTwoFactorTokenAsync(TwoFactorRequestDto request, CancellationToken ct = default)
        {
            
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Result<TwoFactorTokenResponseDto>.Failure("UserId is required.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null || user.IsDeleted)
                return Result<TwoFactorTokenResponseDto>.Failure("User not found or inactive.");

            var setup = await SetupAsync(user); 

           
            var issuer = Uri.EscapeDataString("DigiTekShop");
            var label = Uri.EscapeDataString(user.Email);
            var otpauthUri = $"otpauth://totp/{issuer}:{label}?secret={setup.SecretKey}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";

            // باید درست کنم
            var dto = new TwoFactorTokenResponseDto(
               Token:null,
               DateTimeOffset.MaxValue
            );

            return Result<TwoFactorTokenResponseDto>.Success(dto);
        }

       
        public async Task<Result> UnlockMfaAsync(string userId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Result.Failure("UserId is required.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
                return Result.Failure("User not found or inactive.");

            var record = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserId == user.Id, ct);
            if (record == null)
                return Result.Failure("MFA not configured.");

            if (!record.IsLocked)
                return Result.Failure("MFA is not locked.");

            record.UnlockMfa();
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("MFA unlocked for user {UserId}", user.Id);
            return Result.Success();
        }

      
        public async Task<MfaStatusDto> GetDetailedStatusAsync(User user, CancellationToken ct = default)
        {
            var record = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserId == user.Id, ct);
            
            if (record == null)
                return new MfaStatusDto(false, false, 0, null, null);

            return new MfaStatusDto(
                record.IsEnabled,
                record.IsCurrentlyLocked,
                record.Attempts,
                record.LockedUntil,
                record.LastVerifiedAt
            );
        }

      
        public async Task<int> CleanupExpiredLocksAsync(CancellationToken ct = default)
        {
            try
            {
                var expiredLocks = await _context.UserMfa
                    .Where(mfa => mfa.IsLocked && mfa.LockedUntil.HasValue && mfa.LockedUntil.Value <= DateTime.UtcNow)
                    .ToListAsync(ct);

                foreach (var mfa in expiredLocks)
                {
                    mfa.UnlockMfa();
                }

                if (expiredLocks.Any())
                {
                    _context.UserMfa.UpdateRange(expiredLocks);
                    await _context.SaveChangesAsync(ct);
                    _logger.LogInformation("Cleaned up {Count} expired MFA locks", expiredLocks.Count);
                }

                return expiredLocks.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired MFA locks");
                return 0;
            }
        }
    }
}
