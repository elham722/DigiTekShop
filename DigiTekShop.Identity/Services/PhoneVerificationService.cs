using DigiTekShop.Contracts.DTOs.PhoneVerification;
using DigiTekShop.Contracts.Interfaces.ExternalServices.PhoneSender;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using DigiTekShop.SharedKernel.Guards;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigiTekShop.Identity.Services;

public class PhoneVerificationService
{
    private readonly UserManager<User> _userManager;
    private readonly IPhoneSender _phoneSender;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly PhoneVerificationSettings _settings;
    private readonly ILogger<PhoneVerificationService> _logger;

    public PhoneVerificationService(
        UserManager<User> userManager,
        IPhoneSender phoneSender,
        DigiTekShopIdentityDbContext context,
        IOptions<PhoneVerificationSettings> settings,
        ILogger<PhoneVerificationService> logger)
    {
        _userManager = userManager;
        _phoneSender = phoneSender;
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result> SendVerificationCodeAsync(User user, string phoneNumber)
    {
        if (!_settings.RequirePhoneConfirmation || user.PhoneNumberConfirmed)
            return Result.Success();

        Guard.AgainstInvalidFormat(phoneNumber, _settings.Security.AllowedPhonePattern, nameof(phoneNumber));

        if (_settings.AllowResendCode && !await CanResendCodeAsync(user.Id))
            return Result.Failure($"Please wait {_settings.ResendCooldownMinutes} minutes before resend.");

        var code = GenerateCode(_settings.CodeLength);
        var hash = BCrypt.Net.BCrypt.HashPassword(code);
        var expires = DateTime.UtcNow.AddMinutes(_settings.CodeValidityMinutes);

        var verification = await GetOrCreateVerification(user.Id, hash, expires);
        _context.PhoneVerifications.Update(verification);

        var message = BuildMessage(code);
        var sendResult = await _phoneSender.SendCodeAsync(phoneNumber, code, message);

        if (sendResult.IsFailure) return Result.Failure("Failed to send SMS.");
        await _context.SaveChangesAsync();

        _logger.LogInformation("Verification code sent to {Phone}", phoneNumber);
        return Result.Success();
    }

    public async Task<Result> VerifyCodeAsync(User user, string enteredCode)
    {
        var verification = await _context.PhoneVerifications
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (verification == null) return Result.Failure("No code found.");
        if (verification.IsExpired()) return Result.Failure("Code expired.");
        if (verification.Attempts >= _settings.MaxAttempts) return Result.Failure("Too many attempts.");

        verification.IncrementAttempts(_settings.MaxAttempts);

        if (!BCrypt.Net.BCrypt.Verify(enteredCode, verification.CodeHash))
            return Result.Failure("Invalid code.");

        verification.ResetAttempts(); // ✅ Reset after success
        user.PhoneNumberConfirmed = true;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return Result.Failure(string.Join(", ", update.Errors.Select(e => e.Description)));

        _logger.LogInformation("Phone {Phone} verified for user {User}", user.PhoneNumber, user.Id);
        return Result.Success();
    }

    #region Helpers

    private static string GenerateCode(int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        Span<char> chars = stackalloc char[length];

        for (int i = 0; i < length; i++)
            chars[i] = (char)('0' + (bytes[i] % 10));

        return new string(chars);
    }

    private async Task<PhoneVerification> GetOrCreateVerification(Guid userId, string hash, DateTime expires)
    {
        var existing = await _context.PhoneVerifications
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.ResetCode(hash, expires);
            return existing;
        }

        return PhoneVerification.Create(userId, hash, expires);
    }

    private string BuildMessage(string code)
    {
        var t = _settings.Template;
        var msg = t.UseEnglishTemplate
            ? string.Format(t.CodeTemplateEnglish, code, t.CompanyName)
            : string.Format(t.CodeTemplate, code, t.CompanyName);

        return msg.Length > t.MaxSmsLength ? $"{t.CompanyName} - Code: {code}" : msg;
    }

    public async Task<bool> CanResendCodeAsync(Guid userId)
    {
        var last = await _context.PhoneVerifications
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        return last == null || DateTime.UtcNow >= last.CreatedAt.AddMinutes(_settings.ResendCooldownMinutes);
    }

    #endregion
}
