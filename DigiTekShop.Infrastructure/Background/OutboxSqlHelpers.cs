using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Infrastructure.Background;

internal static class OutboxSqlHelpers
{
    // Claim: هم‌زمانی امن با قفل زمانی
    public static async Task<bool> TryClaimAsync(DbContext db, Guid id, CancellationToken ct)
    {
        // تشخیص نوع جدول بر اساس DbContext
        var tableName = GetTableName(db);
        
        var sql = $"""
                  UPDATE {tableName}
                  SET Status = @p1,
                      LockedUntilUtc = DATEADD(SECOND, @p3, SYSUTCDATETIME()),
                      LockedBy = @p4
                  WHERE Id = @p0
                    AND Status = @p2
                    AND (LockedUntilUtc IS NULL OR LockedUntilUtc <= SYSUTCDATETIME())
                    AND (NextRetryUtc IS NULL OR NextRetryUtc <= SYSUTCDATETIME())
                  """;
        var rows = await db.Database.ExecuteSqlRawAsync(
            sql,
            id,
            1,  // OutboxStatus.Processing = 1
            0,  // OutboxStatus.Pending = 0
            30, // lock window (sec)
            Environment.MachineName);
        return rows == 1;
    }

    public static async Task AckAsync(DbContext db, Guid id, CancellationToken ct)
    {
        // تشخیص نوع جدول بر اساس DbContext
        var tableName = GetTableName(db);
        
        var sql = $"""
                  UPDATE {tableName}
                  SET Status = @p1,
                      ProcessedAtUtc = SYSUTCDATETIME(),
                      Error = NULL,
                      LockedUntilUtc = NULL,
                      LockedBy = NULL
                  WHERE Id = @p0
                  """;
        await db.Database.ExecuteSqlRawAsync(sql, id, 2); // OutboxStatus.Succeeded = 2
    }

    public static async Task NackAsync(DbContext db, Guid id, int attempts, bool giveUp, string error, CancellationToken ct)
    {
        // backoff نمایی: 1, 2, 4, 8, 16, ... دقیقه (سقف 60)
        var delayMinutes = Math.Min(60, (int)Math.Pow(2, Math.Max(0, attempts - 1)));
        var nextStatus = giveUp ? 3 : 0; // OutboxStatus.Failed = 3, Pending = 0

        // تشخیص نوع جدول بر اساس DbContext
        var tableName = GetTableName(db);

        var sql = $"""
                  UPDATE {tableName}
                  SET Status = @p4,
                      Attempts = @p1,
                      Error = LEFT(@p2, 1024),
                      NextRetryUtc = CASE WHEN @p3 = 1 THEN NULL ELSE DATEADD(MINUTE, @p5, SYSUTCDATETIME()) END,
                      LockedUntilUtc = NULL,
                      LockedBy = NULL
                  WHERE Id = @p0
                  """;
        await db.Database.ExecuteSqlRawAsync(sql, id, attempts, error, giveUp ? 1 : 0, nextStatus, delayMinutes);
    }

    private static string GetTableName(DbContext db)
    {
        return db is DigiTekShop.Identity.Context.DigiTekShopIdentityDbContext ? "IdentityOutboxMessages" : "OutboxMessages";
    }
}
