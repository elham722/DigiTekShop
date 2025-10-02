using DigiTekShop.Identity.Models;
using DigiTekShop.Identity.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigiTekShop.Identity.Services;

public class PasswordHistoryService
{
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly PasswordPolicyOptions _policyOptions;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<PasswordHistoryService> _logger;

    public PasswordHistoryService(
        DigiTekShopIdentityDbContext context,
        IOptions<PasswordPolicyOptions> policyOptions,
        IPasswordHasher<User> passwordHasher,
        ILogger<PasswordHistoryService> logger)
    {
        _context = context;
        _policyOptions = policyOptions.Value;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// اضافه کردن پسورد جدید به تاریخچه کاربر
    /// </summary>
    public async Task<bool> AddPasswordToHistoryAsync(User user, string passwordHash)
    {
        try
        {
            var historyEntry = PasswordHistory.Create(user.Id, passwordHash);
            _context.PasswordHistories.Add(historyEntry);

            // حذف تاریخچه‌های قدیمی‌تر از حد مشخص شده
            await CleanupOldHistoriesAsync(user.Id);

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add password to history for user {UserId}", user.Id);
            return false;
        }
    }

    /// <summary>
    /// دریافت تاریخچه پسوردهای کاربر
    /// </summary>
    public async Task<IEnumerable<PasswordHistory>> GetPasswordHistoryAsync(Guid userId, int count = 10)
    {
        return await _context.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.ChangedAt)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// حذف تاریخچه قدیمی‌تر از حد مشخص شده
    /// </summary>
    public async Task<int> CleanupOldHistoriesAsync(Guid userId)
    {
        if (_policyOptions.HistoryDepth <= 0) return 0;

        var histories = await _context.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.ChangedAt)
            .Skip(_policyOptions.HistoryDepth)
            .ToListAsync();

        if (histories.Any())
        {
            _context.PasswordHistories.RemoveRange(histories);
            await _context.SaveChangesAsync();
        }

        return histories.Count;
    }

    /// <summary>
    /// حذف کامل تاریخچه پسوردهای کاربر
    /// </summary>
    public async Task<bool> ClearPasswordHistoryAsync(Guid userId)
    {
        try
        {
            var histories = await _context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .ToListAsync();

            if (histories.Any())
            {
                _context.PasswordHistories.RemoveRange(histories);
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear password history for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// بررسی اینکه آیا پسورد در تاریخچه وجود دارد یا نه
    /// </summary>
    public async Task<bool> DoesPasswordExistInHistoryAsync(User user, string plainPassword)
    {
        if (_policyOptions.HistoryDepth <= 0) return false;

        var histories = await _context.PasswordHistories
            .Where(ph => ph.UserId == user.Id)
            .OrderByDescending(ph => ph.ChangedAt)
            .Take(_policyOptions.HistoryDepth)
            .ToListAsync();

        foreach (var history in histories)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, history.PasswordHash, plainPassword);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return true;
            }
        }

        return false;
    }
}