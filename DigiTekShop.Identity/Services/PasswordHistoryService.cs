using DigiTekShop.Contracts.DTOs.Auth.PasswordHistory;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Models;
using DigiTekShop.Identity.Options;
using DigiTekShop.SharedKernel.Guards;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigiTekShop.Identity.Services;

public sealed class PasswordHistoryService : IPasswordHistoryService
{
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly UserManager<User> _userManager;
    private readonly PasswordPolicyOptions _passwordPolicy;
    private readonly ILogger<PasswordHistoryService> _logger;

    public PasswordHistoryService(
        DigiTekShopIdentityDbContext context,
        IPasswordHasher<User> passwordHasher,
        UserManager<User> userManager,
        IOptions<PasswordPolicyOptions> passwordPolicyOptions,
        ILogger<PasswordHistoryService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _passwordPolicy = passwordPolicyOptions?.Value ?? throw new ArgumentNullException(nameof(passwordPolicyOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> AddAsync(Guid userId, string passwordHash, int? keepLastN = null, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(passwordHash, nameof(passwordHash));

        try
        {
            _context.PasswordHistories.Add(PasswordHistory.Create(userId, passwordHash));

            var historyDepth = keepLastN ?? _passwordPolicy.HistoryDepth;
            
            if (historyDepth > 0)
            {
                await TrimInternalAsync(userId, historyDepth, ct);
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

    public async Task<bool> ExistsInHistoryAsync(Guid userId, string plainPassword, int? maxToCheck = null, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        if (string.IsNullOrWhiteSpace(plainPassword)) return false;
      
        var checkCount = maxToCheck ?? _passwordPolicy.HistoryDepth;
        if (checkCount <= 0) checkCount = _passwordPolicy.HistoryDepth;

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
            .Take(checkCount)
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

  
    public async Task<int> GetHistoryCountAsync(Guid userId, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        
        return await _context.PasswordHistories
            .AsNoTracking()
            .CountAsync(ph => ph.UserId == userId, ct);
    }

   
    public async Task<int> CleanupOldHistoryAsync(Guid userId, TimeSpan olderThan, CancellationToken ct = default)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        
        var cutoffDate = DateTime.UtcNow - olderThan;
        var oldEntries = await _context.PasswordHistories
            .Where(ph => ph.UserId == userId && ph.ChangedAt < cutoffDate)
            .ToListAsync(ct);

        if (oldEntries.Count == 0) return 0;

        _context.PasswordHistories.RemoveRange(oldEntries);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Cleaned up {Count} old password history entries for user {UserId}", oldEntries.Count, userId);
        return oldEntries.Count;
    }

  

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
