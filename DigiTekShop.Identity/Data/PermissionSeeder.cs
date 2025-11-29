using DigiTekShop.SharedKernel.Authorization;

namespace DigiTekShop.Identity.Data;

public static class PermissionSeeder
{
    public static async Task SeedPermissionsAsync(DigiTekShopIdentityDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("Starting permission seeding...");

        var defaults = Permissions.GetAll()
            .Select(p => new
            {
                RawName = p.Name,                                
                CleanName = p.Name?.Trim(),                    
                p.Description
            })
            .Where(p => !string.IsNullOrWhiteSpace(p.CleanName))
            .GroupBy(p => p.CleanName!, StringComparer.OrdinalIgnoreCase)  
            .Select(g => g.First())
            .ToList();

        var cleanNames = defaults
            .Select(p => p.CleanName!)
            .ToList();

        var existing = await context.Permissions
            .Where(p => cleanNames.Contains(p.Name)) 
            .Select(p => p.Name)
            .ToListAsync();

      
        var existingSet = existing
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

     
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

        
        var rolesMap = Permissions.GetRolePermissions();
        var roleNames = rolesMap.Keys.ToList();

        var roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToListAsync();

        var missingRoles = roleNames.Except(roles.Select(r => r.Name)).ToList();
        foreach (var name in missingRoles)
        {
           
            var isSystemRole = name.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                              name.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            
            var isDefaultForNewUsers = name.Equals("Customer", StringComparison.OrdinalIgnoreCase);

            var description = GetRoleDescription(name);
            context.Roles.Add(Role.Create(name, description, isSystemRole, isDefaultForNewUsers));
        }

        if (missingRoles.Any())
            await context.SaveChangesAsync();

       
        roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToListAsync();

        
        var allPermNames = rolesMap.Values
            .SelectMany(v => v)
            .Select(n => n?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
       
        var allPermsFromDb = await context.Permissions
            .Where(p => allPermNames.Contains(p.Name))
            .ToListAsync();
        
        var allPermsSet = allPermsFromDb
            .Select(p => new { Perm = p, CleanName = p.Name?.Trim() })
            .GroupBy(p => p.CleanName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First().Perm)
            .ToList();
        
        var allPerms = allPermsSet;

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

    private static string? GetRoleDescription(string roleName)
    {
        return roleName.ToUpperInvariant() switch
        {
            "SUPERADMIN" => "دسترسی کامل به تمام بخش‌های سیستم. این نقش قابل حذف نیست.",
            "ADMIN" => "مدیر سیستم با دسترسی به بخش‌های مدیریتی. این نقش قابل حذف نیست.",
            "MANAGER" => "مدیر بخش‌های مختلف با دسترسی محدودتر از Admin.",
            "EMPLOYEE" => "کارمند با دسترسی به بخش‌های عملیاتی.",
            "CUSTOMER" => "کاربر عادی فروشگاه. این نقش به صورت خودکار به کاربران جدید اختصاص می‌یابد.",
            _ => null
        };
    }
}

