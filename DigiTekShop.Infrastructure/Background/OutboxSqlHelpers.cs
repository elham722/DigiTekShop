using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Infrastructure.Background;

internal static class OutboxSqlHelpers
{
    // Claim: هم‌زمانی امن با قفل زمانی
    public static async Task<bool> TryClaimAsync(DbContext db, Guid id, CancellationToken ct)
    {
        // تشخیص نوع جدول بر اساس DbContext
        var tableName = GetTableName(db);
        var isIdentityOutbox = IsIdentityOutbox(db);
        
        // IdentityOutboxMessages از int استفاده می‌کند، OutboxMessages از string
        var processingStatus = isIdentityOutbox ? (object)1 : "Processing";
        var pendingStatus = isIdentityOutbox ? (object)0 : "Pending";
        
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
            processingStatus,  // OutboxStatus.Processing
            pendingStatus,     // OutboxStatus.Pending
            30, // lock window (sec)
            Environment.MachineName);
        return rows == 1;
    }

    public static async Task AckAsync(DbContext db, Guid id, CancellationToken ct)
    {
        // تشخیص نوع جدول بر اساس DbContext
        var tableName = GetTableName(db);
        var isIdentityOutbox = IsIdentityOutbox(db);
        
        // IdentityOutboxMessages از int استفاده می‌کند، OutboxMessages از string
        var succeededStatus = isIdentityOutbox ? (object)2 : "Succeeded";
        
        var sql = $"""
                  UPDATE {tableName}
                  SET Status = @p1,
                      ProcessedAtUtc = SYSUTCDATETIME(),
                      Error = NULL,
                      LockedUntilUtc = NULL,
                      LockedBy = NULL
                  WHERE Id = @p0
                  """;
        await db.Database.ExecuteSqlRawAsync(sql, id, succeededStatus); // OutboxStatus.Succeeded
    }

    public static async Task NackAsync(DbContext db, Guid id, int attempts, bool giveUp, string error, CancellationToken ct)
    {
        // backoff نمایی: 1, 2, 4, 8, 16, ... دقیقه (سقف 60)
        var delayMinutes = Math.Min(60, (int)Math.Pow(2, Math.Max(0, attempts - 1)));
        
        // تشخیص نوع جدول بر اساس DbContext
        var tableName = GetTableName(db);
        var isIdentityOutbox = IsIdentityOutbox(db);
        
        // IdentityOutboxMessages از int استفاده می‌کند، OutboxMessages از string
        var nextStatus = giveUp 
            ? (isIdentityOutbox ? (object)3 : "Failed")      // OutboxStatus.Failed
            : (isIdentityOutbox ? (object)0 : "Pending");     // OutboxStatus.Pending

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
        return IsIdentityOutbox(db) ? "IdentityOutboxMessages" : "OutboxMessages";
    }

    private static bool IsIdentityOutbox(DbContext db)
    {
        return db is DigiTekShop.Identity.Context.DigiTekShopIdentityDbContext;
    }
}
