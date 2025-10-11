namespace DigiTekShop.Contracts.Enums.Audit;
    public enum AuditAction
    {
        Created,
        Updated,
        Deleted,
        Login,
        LoginFailed,
        Logout,
        PasswordChange,
        RoleAssignment,
        PermissionChange,
        TokenOperation
    }
