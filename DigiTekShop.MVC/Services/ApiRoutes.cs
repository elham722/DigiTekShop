namespace DigiTekShop.MVC.Services;
public static class ApiRoutes
{
    public const string V = "api/v1";
    public static class Auth
    {

        public const string SentOtp = $"{V}/auth/send-otp";
        public const string VerifyOtp = $"{V}/auth/verify-otp";
        public const string Refresh = $"{V}/auth/refresh";
        public const string Logout = $"{V}/auth/logout";
        public const string Me = $"{V}/auth/me";
    }
}


