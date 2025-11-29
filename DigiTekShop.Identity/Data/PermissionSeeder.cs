using DigiTekShop.SharedKernel.Authorization;

namespace DigiTekShop.Identity.Data;

public static class PermissionSeeder
{
    public static async Task SeedPermissionsAsync(DigiTekShopIdentityDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("Starting permission seeding...");

        // 1) خواندن همه permissionها از کلاس Permissions و پاک‌سازی نام‌ها
        var defaults = Permissions.GetAll()
            .Select(p => new
            {
                RawName = p.Name,                                 // همون چیزی که تعریف کردیم
                CleanName = p.Name?.Trim(),                      // trim
                p.Description
            })
            .Where(p => !string.IsNullOrWhiteSpace(p.CleanName))
            .GroupBy(p => p.CleanName!, StringComparer.OrdinalIgnoreCase)  // اگر دوبار با یک نام تعریف شده، یکی باقی می‌ماند
            .Select(g => g.First())
            .ToList();

        var cleanNames = defaults
            .Select(p => p.CleanName!)
            .ToList();

        // 2) خواندن Permissionهای موجود از DB بر اساس نام تمیز شده
        var existing = await context.Permissions
            .Where(p => cleanNames.Contains(p.Name)) // این هنوز در SQL اجرا می‌شود
            .Select(p => p.Name)
            .ToListAsync();

        // 3) یک HashSet تمیز و Case-Insensitive از نام‌هایی که در DB داریم
        var existingSet = existing
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 4) فقط آن permissionهایی که بعد از Trim/Case-Insensitive در DB نیستند، اضافه شوند
        var toInsert = defaults
            .Where(p => !existingSet.Contains(p.CleanName!))
            .Select(p => Permission.Create(p.CleanName!, p.Description))
            .ToList();

        if (toInsert.Count > 0)
        {
            context.Permissions.AddRange(toInsert);
            await context.SaveChangesAsync();
            logger?.LogInformation("Inserted {Count} permissions.", toInsert.Count);
        }
        else
        {
            logger?.LogInformation("No new permissions to insert.");
        }
    }



    public static async Task SeedRolesAsync(DigiTekShopIdentityDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("Starting role seeding...");

        // Use centralized Permissions class
        var rolesMap = Permissions.GetRolePermissions();
        var roleNames = rolesMap.Keys.ToList();

        // نقش‌های موجود
        var roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToListAsync();

        // ایجاد نقش‌های جدید
        var missingRoles = roleNames.Except(roles.Select(r => r.Name)).ToList();
        foreach (var name in missingRoles)
            context.Roles.Add(Role.Create(name));

        if (missingRoles.Any())
            await context.SaveChangesAsync();

        // دوباره همه نقش‌ها را واکشی کن تا Id داشته باشند
        roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToListAsync();

        // همه‌ی Permissionها یکجا (با normalization برای جلوگیری از duplicate)
        var allPermNames = rolesMap.Values
            .SelectMany(v => v)
            .Select(n => n?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        // خواندن از DB و ساخت HashSet برای مقایسه case-insensitive
        var allPermsFromDb = await context.Permissions
            .Where(p => allPermNames.Contains(p.Name))
            .ToListAsync();
        
        var allPermsSet = allPermsFromDb
            .Select(p => new { Perm = p, CleanName = p.Name?.Trim() })
            .GroupBy(p => p.CleanName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First().Perm)
            .ToList();
        
        var allPerms = allPermsSet;

        // RolePermissionهای موجود یکجا
        var roleIds = roles.Select(r => r.Id).ToList();
        var rolePermsExisting = await context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync();

        var toAdd = new List<RolePermission>();
        foreach (var role in roles)
        {
            var wanted = rolesMap[role.Name!]
                .Select(n => n?.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            
            foreach (var permName in wanted)
            {
                // مقایسه case-insensitive برای پیدا کردن permission
                var perm = allPerms.FirstOrDefault(p => 
                    string.Equals(p.Name?.Trim(), permName, StringComparison.OrdinalIgnoreCase));
                
                if (perm == null)
                {
                    logger?.LogWarning("Permission '{Perm}' not found for role '{Role}'", permName, role.Name);
                    continue;
                }

                var exists = rolePermsExisting.Any(rp => rp.RoleId == role.Id && rp.PermissionId == perm.Id);
                if (!exists)
                    toAdd.Add(RolePermission.Create(role.Id, perm.Id));
            }
        }

        if (toAdd.Count > 0)
        {
            context.RolePermissions.AddRange(toAdd);
            await context.SaveChangesAsync();
            logger?.LogInformation("Assigned {Count} role-permissions.", toAdd.Count);
        }
        else
        {
            logger?.LogInformation("No new role-permissions to assign.");
        }
    }


    public static async Task SeedAllAsync(DigiTekShopIdentityDbContext context, ILogger? logger = null)
    {
        await SeedPermissionsAsync(context, logger);
        await SeedRolesAsync(context, logger);
    }
}

