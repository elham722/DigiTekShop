using DigiTekShop.Contracts.DTOs.Auth.PasswordHistory;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Guards;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Identity.Services;

public sealed class PasswordHistoryService : IPasswordHistoryService
{
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<PasswordHistoryService> _logger;

    public PasswordHistoryService(
        DigiTekShopIdentityDbContext context,
        IPasswordHasher<User> passwordHasher,
        UserManager<User> userManager,
        ILogger<PasswordHistoryService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> AddAsync(Guid userId, string passwordHash, int? keepLastN = null, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(passwordHash, nameof(passwordHash));

        try
        {
            _context.PasswordHistories.Add(PasswordHistory.Create(userId, passwordHash));

            if (keepLastN is int k && k > 0)
            {
                // Trim در همان تراکنش/Save
                await TrimInternalAsync(userId, k, ct);
            }

            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add password to history for user {UserId}", userId);
            return false;
        }
    }

    public async Task<IReadOnlyList<PasswordHistoryEntryDto>> GetAsync(Guid userId, int count = 10, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        if (count <= 0) count = 10;

        var items = await _context.PasswordHistories
            .AsNoTracking()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.ChangedAt)
            .Take(count)
            .Select(ph => new PasswordHistoryEntryDto(ph.ChangedAt))
            .ToListAsync(ct);

        return items;
    }

    public async Task<int> TrimAsync(Guid userId, int keepLastN, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        if (keepLastN < 0) keepLastN = 0;

        var removed = await TrimInternalAsync(userId, keepLastN, ct);
        await _context.SaveChangesAsync(ct);
        return removed;
    }

    public async Task<bool> ClearAsync(Guid userId, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));

        try
        {
            var all = await _context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .ToListAsync(ct);

            if (all.Count == 0) return true;

            _context.PasswordHistories.RemoveRange(all);
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear password history for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ExistsInHistoryAsync(Guid userId, string plainPassword, int maxToCheck = 10, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        if (string.IsNullOrWhiteSpace(plainPassword)) return false;
        if (maxToCheck <= 0) maxToCheck = 10;

        // ✅ دیگه new User نمی‌زنیم؛ کاربر واقعی رو میاریم
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            _logger.LogWarning("ExistsInHistoryAsync: user not found {UserId}", userId);
            return false;
        }

        var lastHashes = await _context.PasswordHistories
            .AsNoTracking()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.ChangedAt)
            .Take(maxToCheck)
            .Select(ph => ph.PasswordHash)
            .ToListAsync(ct);

        foreach (var oldHash in lastHashes)
        {
            var verification = _passwordHasher.VerifyHashedPassword(user, oldHash, plainPassword);
            if (verification is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
                return true;
        }

        return false;
    }

    // ---------- Private ----------

    private async Task<int> TrimInternalAsync(Guid userId, int keepLastN, CancellationToken ct)
    {
        var oldOnes = await _context.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.ChangedAt)
            .Skip(keepLastN)
            .ToListAsync(ct);

        if (oldOnes.Count == 0) return 0;

        _context.PasswordHistories.RemoveRange(oldOnes);
        return oldOnes.Count;
    }
}
