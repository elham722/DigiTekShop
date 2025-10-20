using DigiTekShop.Contracts.DTOs.Auth.PasswordHistory;
using DigiTekShop.Contracts.Options.Password;

namespace DigiTekShop.Identity.Services.Password;

public sealed class PasswordHistoryService : IPasswordHistoryService
{
    private static class Events
    {
        public static readonly EventId Add = new(51001, nameof(AddAsync));
        public static readonly EventId Get = new(51002, nameof(GetAsync));
        public static readonly EventId Trim = new(51003, nameof(TrimAsync));
        public static readonly EventId Clear = new(51004, nameof(ClearAsync));
        public static readonly EventId Exists = new(51005, nameof(ExistsInHistoryAsync));
        public static readonly EventId Count = new(51006, nameof(GetHistoryCountAsync));
        public static readonly EventId Cleanup = new(51007, nameof(CleanupOldHistoryAsync));
    }

    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IDateTimeProvider _time;
    private readonly PasswordPolicyOptions _policy;
    private readonly ILogger<PasswordHistoryService> _log;

    public PasswordHistoryService(
        DigiTekShopIdentityDbContext db,
        IPasswordHasher<User> hasher,
        IDateTimeProvider time,
        IOptions<PasswordPolicyOptions> policy,
        ILogger<PasswordHistoryService> log)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _policy = policy?.Value ?? throw new ArgumentNullException(nameof(policy));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    #region Compiled queries

    private static readonly Func<DigiTekShopIdentityDbContext, Guid, int, IAsyncEnumerable<string>>
        Q_LastHashes = EF.CompileAsyncQuery((DigiTekShopIdentityDbContext ctx, Guid userId, int take) =>
            ctx.PasswordHistories
                .AsNoTracking()
                .Where(ph => ph.UserId == userId)
                .OrderByDescending(ph => ph.ChangedAtUtc)
                .Take(take)
                .Select(ph => ph.PasswordHash));

    private static readonly Func<DigiTekShopIdentityDbContext, Guid, int, IAsyncEnumerable<PasswordHistoryEntryDto>>
        Q_LastEntries = EF.CompileAsyncQuery((DigiTekShopIdentityDbContext ctx, Guid userId, int take) =>
            ctx.PasswordHistories
                .AsNoTracking()
                .Where(ph => ph.UserId == userId)
                .OrderByDescending(ph => ph.ChangedAtUtc)
                .Take(take)
                .Select(ph => new PasswordHistoryEntryDto(ph.ChangedAtUtc)));

    private static readonly Func<DigiTekShopIdentityDbContext, Guid, Task<int>>
        Q_Count = EF.CompileAsyncQuery((DigiTekShopIdentityDbContext ctx, Guid userId) =>
            ctx.PasswordHistories.Count(ph => ph.UserId == userId));

    #endregion

    public async Task<bool> AddAsync(Guid userId, string passwordHash, int? keepLastN = null, CancellationToken ct = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(passwordHash)) return false;

        try
        {
            var now = _time.UtcNow;
            _db.PasswordHistories.Add(PasswordHistory.Create(userId, passwordHash, now));

            await _db.SaveChangesAsync(ct); 

            var depth = NormalizeDepth(keepLastN ?? _policy.HistoryDepth);
            if (depth > 0)
                await TrimServerSideAsync(userId, depth, ct); 

            _log.LogInformation(Events.Add, "Password history added. user={UserId}", userId);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Add, ex, "Failed to add password history. user={UserId}", userId);
            return false;
        }
    }


    public async Task<IReadOnlyList<PasswordHistoryEntryDto>> GetAsync(Guid userId, int count = 10, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return Array.Empty<PasswordHistoryEntryDto>();
        var take = count <= 0 ? 10 : count;

        var list = new List<PasswordHistoryEntryDto>(take);
        await foreach (var dto in Q_LastEntries(_db, userId, take).WithCancellation(ct))
            list.Add(dto);

        return list;
    }

    public async Task<int> TrimAsync(Guid userId, int keepLastN, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return 0;
        var depth = NormalizeDepth(keepLastN);
        var removed = await TrimServerSideAsync(userId, depth, ct);
        await _db.SaveChangesAsync(ct);
        _log.LogInformation(Events.Trim, "Trimmed password history. user={UserId}, kept={Keep}", userId, depth);
        return removed;
    }

    public async Task<bool> ClearAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return true;

        try
        {
            var deleted = await _db.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .ExecuteDeleteAsync(ct);

            _log.LogInformation(Events.Clear, "Cleared password history. user={UserId}, deleted={Count}", userId, deleted);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.LogError(Events.Clear, ex, "Failed to clear password history. user={UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ExistsInHistoryAsync(
        Guid userId, string plainPassword, int? maxToCheck = null, CancellationToken ct = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(plainPassword))
            return false;

        var take = NormalizeDepth(maxToCheck ?? _policy.HistoryDepth);
        if (take == 0) return false;

        await foreach (var oldHash in Q_LastHashes(_db, userId, take).WithCancellation(ct))
        {
            var result = _hasher.VerifyHashedPassword(
                user: default!,
                hashedPassword: oldHash,
                providedPassword: plainPassword);

            if (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
                return true;
        }

        return false;
    }


    public Task<int> GetHistoryCountAsync(Guid userId, CancellationToken ct = default)
        => userId == Guid.Empty ? Task.FromResult(0) : Q_Count(_db, userId);

    public async Task<int> CleanupOldHistoryAsync(Guid userId, TimeSpan olderThan, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return 0;
        if (olderThan <= TimeSpan.Zero) return 0;

        var cutoffUtc = _time.UtcNow - olderThan;

        var deleted = await _db.PasswordHistories
            .Where(ph => ph.UserId == userId && ph.ChangedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            _log.LogInformation(Events.Cleanup, "Cleaned old password history. user={UserId}, deleted={Count}", userId, deleted);

        return deleted;
    }


    #region Helpers

    private static int NormalizeDepth(int depth)
        => depth < 0 ? 0 : depth;


    private async Task<int> TrimServerSideAsync(Guid userId, int keepLastN, CancellationToken ct)
    {

        if (keepLastN == int.MaxValue) return 0;


        var query =
            from ph in _db.PasswordHistories
            where ph.UserId == userId
            orderby ph.ChangedAtUtc descending
            select ph.Id;

        var idsToKeep = await query.Take(keepLastN).ToListAsync(ct);

        var deleted = await _db.PasswordHistories
            .Where(ph => ph.UserId == userId && !idsToKeep.Contains(ph.Id))
            .ExecuteDeleteAsync(ct);

        return deleted;
    }

    #endregion

}
