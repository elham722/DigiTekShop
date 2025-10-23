namespace DigiTekShop.Identity.Data;

public static class PermissionSeeder
{
    public static async Task SeedPermissionsAsync(DigiTekShopIdentityDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("Starting permission seeding...");

        var defaults = GetDefaultPermissions();
        var names = defaults.Select(p => p.Name).ToList();

        var existing = await context.Permissions
            .Where(p => names.Contains(p.Name))
            .Select(p => p.Name)
            .ToListAsync();

        var toInsert = defaults
            .Where(p => !existing.Contains(p.Name))
            .Select(p => Permission.Create(p.Name, p.Description))
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

        var rolesMap = GetDefaultRolesWithPermissions();
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

        // همه‌ی Permissionها یکجا
        var allPermNames = rolesMap.Values.SelectMany(v => v).Distinct().ToList();
        var allPerms = await context.Permissions
            .Where(p => allPermNames.Contains(p.Name))
            .ToListAsync();

        // RolePermissionهای موجود یکجا
        var roleIds = roles.Select(r => r.Id).ToList();
        var rolePermsExisting = await context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync();

        var toAdd = new List<RolePermission>();
        foreach (var role in roles)
        {
            var wanted = rolesMap[role.Name];
            foreach (var permName in wanted)
            {
                var perm = allPerms.FirstOrDefault(p => p.Name == permName);
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

    private static List<(string Name, string Description)> GetDefaultPermissions()
    {
        return new List<(string, string)>
        {
            // Admin Permissions
            ("Admin.Roles.Manage", "Manage roles (CRUD)"),
            ("Admin.Users.Manage", "Manage users (CRUD, assign roles)"),
            ("Admin.Permissions.Manage", "Manage permissions (CRUD, assign to roles/users)"),
            ("Admin.Permissions.View", "View all permissions"),
            ("Admin.System.Configure", "Configure system settings"),

            // Products Permissions
            ("Products.View", "View products"),
            ("Products.Create", "Create new products"),
            ("Products.Edit", "Edit existing products"),
            ("Products.Delete", "Delete products"),
            ("Products.ViewInventory", "View product inventory details (sensitive)"),
            ("Products.ManageInventory", "Manage product inventory"),

            // Orders Permissions
            ("Orders.View", "View own orders"),
            ("Orders.ViewAll", "View all orders (admin/manager)"),
            ("Orders.Create", "Create new orders"),
            ("Orders.UpdateStatus", "Update order status"),
            ("Orders.Cancel", "Cancel orders"),
            ("Orders.Delete", "Delete orders (admin only)"),
            ("Orders.ViewFinancials", "View order financial details (sensitive)"),

            // Customers Permissions
            ("Customers.View", "View customers"),
            ("Customers.Create", "Create new customers"),
            ("Customers.Edit", "Edit customer information"),
            ("Customers.Delete", "Delete customers"),
            ("Customers.ViewSensitive", "View sensitive customer data (payment info, etc.)"),

            // Reports Permissions
            ("Reports.View", "View reports"),
            ("Reports.ViewSales", "View sales reports"),
            ("Reports.ViewFinancial", "View financial reports"),
            ("Reports.Export", "Export reports"),

            // Security & Audit Permissions
            ("Security.ViewLogs", "View security logs"),
            ("Security.ViewAuditTrail", "View audit trail"),
            ("Security.ManageDevices", "Manage user devices"),
            ("Security.ManageSessions", "Manage user sessions"),

            // Settings Permissions
            ("Settings.View", "View settings"),
            ("Settings.Edit", "Edit settings"),
        };
    }

   
    private static Dictionary<string, List<string>> GetDefaultRolesWithPermissions()
    {
        return new Dictionary<string, List<string>>
        {
            // SuperAdmin - تمام دسترسی‌ها
            {
                "SuperAdmin",
                new List<string>
                {
                    // Admin
                    "Admin.Roles.Manage",
                    "Admin.Users.Manage",
                    "Admin.Permissions.Manage",
                    "Admin.Permissions.View",
                    "Admin.System.Configure",

                    // Products
                    "Products.View",
                    "Products.Create",
                    "Products.Edit",
                    "Products.Delete",
                    "Products.ViewInventory",
                    "Products.ManageInventory",

                    // Orders
                    "Orders.View",
                    "Orders.ViewAll",
                    "Orders.Create",
                    "Orders.UpdateStatus",
                    "Orders.Cancel",
                    "Orders.Delete",
                    "Orders.ViewFinancials",

                    // Customers
                    "Customers.View",
                    "Customers.Create",
                    "Customers.Edit",
                    "Customers.Delete",
                    "Customers.ViewSensitive",

                    // Reports
                    "Reports.View",
                    "Reports.ViewSales",
                    "Reports.ViewFinancial",
                    "Reports.Export",

                    // Security
                    "Security.ViewLogs",
                    "Security.ViewAuditTrail",
                    "Security.ManageDevices",
                    "Security.ManageSessions",

                    // Settings
                    "Settings.View",
                    "Settings.Edit",
                }
            },

            // Admin - دسترسی‌های مدیریتی (بدون تنظیمات حساس)
            {
                "Admin",
                new List<string>
                {
                    "Admin.Users.Manage",
                    "Admin.Roles.Manage",
                    "Admin.Permissions.View",

                    "Products.View",
                    "Products.Create",
                    "Products.Edit",
                    "Products.ViewInventory",
                    "Products.ManageInventory",

                    "Orders.ViewAll",
                    "Orders.UpdateStatus",
                    "Orders.Cancel",
                    "Orders.ViewFinancials",

                    "Customers.View",
                    "Customers.Create",
                    "Customers.Edit",

                    "Reports.View",
                    "Reports.ViewSales",
                    "Reports.ViewFinancial",
                    "Reports.Export",

                    "Security.ViewLogs",
                }
            },

            // Manager - مدیریت عملیاتی
            {
                "Manager",
                new List<string>
                {
                    "Products.View",
                    "Products.Create",
                    "Products.Edit",
                    "Products.ViewInventory",

                    "Orders.ViewAll",
                    "Orders.UpdateStatus",
                    "Orders.Cancel",

                    "Customers.View",
                    "Customers.Edit",

                    "Reports.View",
                    "Reports.ViewSales",
                    "Reports.Export",
                }
            },

            // Employee - کارمند عادی
            {
                "Employee",
                new List<string>
                {
                    "Products.View",
                    "Products.Create",

                    "Orders.ViewAll",
                    "Orders.Create",
                    "Orders.UpdateStatus",

                    "Customers.View",
                    "Customers.Create",
                }
            },

            // Customer - مشتری
            {
                "Customer",
                new List<string>
                {
                    "Products.View",

                    "Orders.View",
                    "Orders.Create",
                    "Orders.Cancel",
                }
            },
        };
    }


    public static async Task SeedAllAsync(DigiTekShopIdentityDbContext context, ILogger? logger = null)
    {
        await SeedPermissionsAsync(context, logger);
        await SeedRolesAsync(context, logger);
    }
}

