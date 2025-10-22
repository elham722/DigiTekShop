namespace DigiTekShop.MVC.Services;
public static class ApiRoutes
{
    public const string V = "api/v1";
    public static class Auth
    {
        public const string Login = $"{V}/auth/login";
        public const string VerifyMfa = $"{V}/auth/verify-mfa";
        public const string Refresh = $"{V}/auth/refresh";
        public const string Logout = $"{V}/auth/logout";
        public const string Me = $"{V}/auth/me";
    }
}


