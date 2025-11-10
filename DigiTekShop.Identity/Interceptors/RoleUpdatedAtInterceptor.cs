using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigiTekShop.Identity.Interceptors;

/// <summary>
/// Interceptor to automatically update UpdatedAt timestamp for Role entities when they are modified
/// </summary>
public sealed class RoleUpdatedAtInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateRoleTimestamps(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateRoleTimestamps(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void UpdateRoleTimestamps(DbContext? context)
    {
        if (context is not DigiTekShopIdentityDbContext dbContext)
            return;

        var now = DateTimeOffset.UtcNow;

        var modifiedRoleEntries = dbContext.ChangeTracker.Entries<Role>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in modifiedRoleEntries)
        {
            // Only update UpdatedAt if it hasn't been explicitly changed
            // This allows UpdateName() to set it explicitly, but catches other modifications
            var updatedAtProperty = entry.Property(r => r.UpdatedAt);
            if (!updatedAtProperty.IsModified || updatedAtProperty.CurrentValue == null)
            {
                updatedAtProperty.CurrentValue = now;
            }
        }
    }
}

