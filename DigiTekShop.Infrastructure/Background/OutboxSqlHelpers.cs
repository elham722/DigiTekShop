using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Infrastructure.Background;

internal static class OutboxSqlHelpers
{
    // Claim: هم‌زمانی امن با قفل زمانی
    public static async Task<bool> TryClaimAsync(DbContext db, Guid id, CancellationToken ct)
    {
        var sql = """
                  UPDATE OutboxMessages
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
            "Processing",
            "Pending",
            30,                 // lock window (sec)
            Environment.MachineName);
        return rows == 1;
    }

    public static async Task AckAsync(DbContext db, Guid id, CancellationToken ct)
    {
        var sql = """
                  UPDATE OutboxMessages
                  SET Status = @p1,
                      ProcessedAtUtc = SYSUTCDATETIME(),
                      Error = NULL,
                      LockedUntilUtc = NULL,
                      LockedBy = NULL
                  WHERE Id = @p0
                  """;
        await db.Database.ExecuteSqlRawAsync(sql, id, "Succeeded");
    }

    public static async Task NackAsync(DbContext db, Guid id, int attempts, bool giveUp, string error, CancellationToken ct)
    {
        // backoff نمایی: 1, 2, 4, 8, 16, ... دقیقه (سقف 60)
        var delayMinutes = Math.Min(60, (int)Math.Pow(2, Math.Max(0, attempts - 1)));
        var next = giveUp ? "Failed" : "Pending";

        var sql = """
                  UPDATE OutboxMessages
                  SET Status = @p4,
                      Attempts = @p1,
                      Error = LEFT(@p2, 1000),
                      NextRetryUtc = CASE WHEN @p3 = 1 THEN NULL ELSE DATEADD(MINUTE, @p5, SYSUTCDATETIME()) END,
                      LockedUntilUtc = NULL,
                      LockedBy = NULL
                  WHERE Id = @p0
                  """;
        await db.Database.ExecuteSqlRawAsync(sql, id, attempts, error, giveUp ? 1 : 0, next, delayMinutes);
    }
}
