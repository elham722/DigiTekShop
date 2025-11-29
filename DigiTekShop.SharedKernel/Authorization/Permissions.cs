namespace DigiTekShop.SharedKernel.Authorization;

/// <summary>
/// Central permission constants for DigiTekShop.
/// All permission strings are domain-oriented and follow the pattern: "Module.Action"
/// </summary>
/// <remarks>
/// These constants are used in:
/// - JWT Claims (permission type)
/// - [RequirePermission] attribute
/// - UI permission checks
/// - Database seeding
/// </remarks>
public static class Permissions
{
    /// <summary>
    /// The claim type used for permissions in JWT tokens
    /// </summary>
    public const string ClaimType = "permission";

    #region Admin

    public static class Admin
    {
        public const string UsersManage = "admin.users.manage";
        public const string UsersView = "admin.users.view";
        public const string UsersLock = "admin.users.lock";
        public const string UsersDelete = "admin.users.delete";
        public const string UsersRolesAssign = "admin.users.roles.assign";

        public const string RolesManage = "admin.roles.manage";
        public const string RolesView = "admin.roles.view";

        public const string PermissionsManage = "admin.permissions.manage";
        public const string PermissionsView = "admin.permissions.view";

        public const string SystemConfigure = "admin.system.configure";
        public const string SystemHealth = "admin.system.health";
    }

    #endregion

    #region Catalog (Products & Categories)

    public static class Catalog
    {
        public const string View = "catalog.view";
        public const string Create = "catalog.create";
        public const string Edit = "catalog.edit";
        public const string Delete = "catalog.delete";
        public const string Publish = "catalog.publish";
        public const string ManagePricing = "catalog.pricing.manage";
        public const string ViewInventory = "catalog.inventory.view";
        public const string ManageInventory = "catalog.inventory.manage";
    }

    #endregion

    #region Orders

    public static class Orders
    {
        public const string ViewOwn = "orders.view.own";
        public const string ViewAll = "orders.view.all";
        public const string Create = "orders.create";
        public const string UpdateStatus = "orders.status.update";
        public const string Cancel = "orders.cancel";
        public const string Delete = "orders.delete";
        public const string Refund = "orders.refund";
        public const string ViewFinancials = "orders.financials.view";
        public const string Export = "orders.export";
    }

    #endregion

    #region Customers

    public static class Customers
    {
        public const string View = "customers.view";
        public const string Create = "customers.create";
        public const string Edit = "customers.edit";
        public const string Delete = "customers.delete";
        public const string ViewSensitive = "customers.sensitive.view";
        public const string Export = "customers.export";
    }

    #endregion

    #region Reports

    public static class Reports
    {
        public const string View = "reports.view";
        public const string Sales = "reports.sales";
        public const string Financial = "reports.financial";
        public const string Inventory = "reports.inventory";
        public const string Export = "reports.export";
    }

    #endregion

    #region Security & Audit

    public static class Security
    {
        public const string ViewLogs = "security.logs.view";
        public const string ViewAuditTrail = "security.audit.view";
        public const string ManageDevices = "security.devices.manage";
        public const string ManageSessions = "security.sessions.manage";
        public const string ViewSecurityEvents = "security.events.view";
    }

    #endregion

    #region Settings

    public static class Settings
    {
        public const string View = "settings.view";
        public const string Edit = "settings.edit";
        public const string ManagePaymentGateways = "settings.payment.manage";
        public const string ManageShipping = "settings.shipping.manage";
        public const string ManageNotifications = "settings.notifications.manage";
    }

    #endregion

    #region Content (CMS)

    public static class Content
    {
        public const string View = "content.view";
        public const string Create = "content.create";
        public const string Edit = "content.edit";
        public const string Delete = "content.delete";
        public const string Publish = "content.publish";
    }

    #endregion

    #region Discounts & Promotions

    public static class Discounts
    {
        public const string View = "discounts.view";
        public const string Create = "discounts.create";
        public const string Edit = "discounts.edit";
        public const string Delete = "discounts.delete";
        public const string Activate = "discounts.activate";
    }

    #endregion

    /// <summary>
    /// Gets all defined permissions for seeding
    /// </summary>
    public static IReadOnlyList<(string Name, string Description)> GetAll()
    {
        return new List<(string, string)>
        {
            // Admin
            (Admin.UsersManage, "مدیریت کامل کاربران"),
            (Admin.UsersView, "مشاهده لیست کاربران"),
            (Admin.UsersLock, "قفل/آنلاک کاربران"),
            (Admin.UsersDelete, "حذف کاربران"),
            (Admin.UsersRolesAssign, "اختصاص نقش به کاربران"),
            (Admin.RolesManage, "مدیریت نقش‌ها"),
            (Admin.RolesView, "مشاهده نقش‌ها"),
            (Admin.PermissionsManage, "مدیریت دسترسی‌ها"),
            (Admin.PermissionsView, "مشاهده دسترسی‌ها"),
            (Admin.SystemConfigure, "پیکربندی سیستم"),
            (Admin.SystemHealth, "مشاهده سلامت سیستم"),

            // Catalog
            (Catalog.View, "مشاهده محصولات"),
            (Catalog.Create, "ایجاد محصول"),
            (Catalog.Edit, "ویرایش محصول"),
            (Catalog.Delete, "حذف محصول"),
            (Catalog.Publish, "انتشار/عدم انتشار محصول"),
            (Catalog.ManagePricing, "مدیریت قیمت‌گذاری"),
            (Catalog.ViewInventory, "مشاهده موجودی"),
            (Catalog.ManageInventory, "مدیریت موجودی"),

            // Orders
            (Orders.ViewOwn, "مشاهده سفارشات خود"),
            (Orders.ViewAll, "مشاهده همه سفارشات"),
            (Orders.Create, "ایجاد سفارش"),
            (Orders.UpdateStatus, "تغییر وضعیت سفارش"),
            (Orders.Cancel, "لغو سفارش"),
            (Orders.Delete, "حذف سفارش"),
            (Orders.Refund, "استرداد وجه"),
            (Orders.ViewFinancials, "مشاهده اطلاعات مالی سفارش"),
            (Orders.Export, "خروجی سفارشات"),

            // Customers
            (Customers.View, "مشاهده مشتریان"),
            (Customers.Create, "ایجاد مشتری"),
            (Customers.Edit, "ویرایش مشتری"),
            (Customers.Delete, "حذف مشتری"),
            (Customers.ViewSensitive, "مشاهده اطلاعات حساس مشتری"),
            (Customers.Export, "خروجی مشتریان"),

            // Reports
            (Reports.View, "مشاهده گزارشات"),
            (Reports.Sales, "گزارش فروش"),
            (Reports.Financial, "گزارش مالی"),
            (Reports.Inventory, "گزارش موجودی"),
            (Reports.Export, "خروجی گزارشات"),

            // Security
            (Security.ViewLogs, "مشاهده لاگ‌های سیستم"),
            (Security.ViewAuditTrail, "مشاهده ردپای تغییرات"),
            (Security.ManageDevices, "مدیریت دستگاه‌های کاربران"),
            (Security.ManageSessions, "مدیریت نشست‌های کاربران"),
            (Security.ViewSecurityEvents, "مشاهده رویدادهای امنیتی"),

            // Settings
            (Settings.View, "مشاهده تنظیمات"),
            (Settings.Edit, "ویرایش تنظیمات"),
            (Settings.ManagePaymentGateways, "مدیریت درگاه‌های پرداخت"),
            (Settings.ManageShipping, "مدیریت حمل و نقل"),
            (Settings.ManageNotifications, "مدیریت اعلان‌ها"),

            // Content
            (Content.View, "مشاهده محتوا"),
            (Content.Create, "ایجاد محتوا"),
            (Content.Edit, "ویرایش محتوا"),
            (Content.Delete, "حذف محتوا"),
            (Content.Publish, "انتشار محتوا"),

            // Discounts
            (Discounts.View, "مشاهده تخفیف‌ها"),
            (Discounts.Create, "ایجاد تخفیف"),
            (Discounts.Edit, "ویرایش تخفیف"),
            (Discounts.Delete, "حذف تخفیف"),
            (Discounts.Activate, "فعال/غیرفعال کردن تخفیف"),
        };
    }

    /// <summary>
    /// Gets default role-permission mappings for seeding
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetRolePermissions()
    {
        return new Dictionary<string, IReadOnlyList<string>>
        {
            ["SuperAdmin"] = GetAll().Select(p => p.Name).ToList(),

            ["Admin"] = new[]
            {
                Admin.UsersView, Admin.UsersManage, Admin.UsersLock, Admin.UsersRolesAssign,
                Admin.RolesView, Admin.RolesManage,
                Admin.PermissionsView,
                Admin.SystemHealth,

                Catalog.View, Catalog.Create, Catalog.Edit, Catalog.Delete, Catalog.Publish,
                Catalog.ManagePricing, Catalog.ViewInventory, Catalog.ManageInventory,

                Orders.ViewAll, Orders.UpdateStatus, Orders.Cancel, Orders.Refund, Orders.ViewFinancials, Orders.Export,

                Customers.View, Customers.Create, Customers.Edit, Customers.ViewSensitive, Customers.Export,

                Reports.View, Reports.Sales, Reports.Financial, Reports.Inventory, Reports.Export,

                Security.ViewLogs, Security.ViewAuditTrail, Security.ViewSecurityEvents,

                Settings.View,

                Content.View, Content.Create, Content.Edit, Content.Delete, Content.Publish,

                Discounts.View, Discounts.Create, Discounts.Edit, Discounts.Delete, Discounts.Activate,
            },

            ["Manager"] = new[]
            {
                Admin.UsersView,

                Catalog.View, Catalog.Create, Catalog.Edit, Catalog.Publish,
                Catalog.ViewInventory, Catalog.ManageInventory,

                Orders.ViewAll, Orders.UpdateStatus, Orders.Cancel, Orders.Export,

                Customers.View, Customers.Edit,

                Reports.View, Reports.Sales, Reports.Inventory, Reports.Export,

                Content.View, Content.Create, Content.Edit,

                Discounts.View, Discounts.Create, Discounts.Edit, Discounts.Activate,
            },

            ["Employee"] = new[]
            {
                Catalog.View, Catalog.Create,
                Catalog.ViewInventory,

                Orders.ViewAll, Orders.Create, Orders.UpdateStatus,

                Customers.View, Customers.Create,

                Content.View,
            },

            ["Customer"] = new[]
            {
                Catalog.View,
                Orders.ViewOwn, Orders.Create, Orders.Cancel,
            },
        };
    }
}

