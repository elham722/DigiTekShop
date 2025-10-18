using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Infrastructure.Background;

internal static class OutboxSqlHelpers
{
    public static async Task<bool> TryClaimAsync(DbContext db, Guid id, CancellationToken ct)
    {
        var sql = """
                  UPDATE OutboxMessages
                  SET Status = @p1
                  WHERE Id = @p0 AND Status = @p2
                  """;
        // Processing = "Processing", Pending = "Pending" چون Enum را به string ذخیره می‌کنی
        var rows = await db.Database.ExecuteSqlRawAsync(
            sql,
            id,
            "Processing",
            "Pending");
        return rows == 1;
    }

    public static async Task AckAsync(DbContext db, Guid id, CancellationToken ct)
    {
        var sql = """
                  UPDATE OutboxMessages
                  SET Status = @p1, ProcessedAtUtc = SYSUTCDATETIME(), Error = NULL
                  WHERE Id = @p0
                  """;
        await db.Database.ExecuteSqlRawAsync(sql, id, "Succeeded");
    }

    public static async Task NackAsync(DbContext db, Guid id, int attempts, bool giveUp, string error, CancellationToken ct)
    {
        var next = giveUp ? "Failed" : "Pending";
        var sql = """
                  UPDATE OutboxMessages
                  SET Status = @p3, Attempts = @p1, Error = @p2
                  WHERE Id = @p0
                  """;
        await db.Database.ExecuteSqlRawAsync(sql, id, attempts, error, next);
    }
}