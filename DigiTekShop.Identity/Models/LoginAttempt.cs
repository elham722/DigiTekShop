using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Identity.Models;
    public class LoginAttempt
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid? UserId { get; private set; }
        public string? LoginNameOrEmail { get; private set; }
        public DateTime AttemptedAt { get; private set; } 
        public LoginStatus Status { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public string? LoginNameOrEmailNormalized { get; private set; }

    private LoginAttempt() { }

        public static LoginAttempt Create(Guid? userId, LoginStatus status, string? ipAddress = null, string? userAgent = null, string? loginNameOrEmail = null)
        {
            return new LoginAttempt
            {
                UserId = userId,
                LoginNameOrEmail = loginNameOrEmail,
                Status = status,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };
        }

        public void UpdateAttempt(LoginStatus status, string? ipAddress = null, string? userAgent = null)
        {
            Status = status;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            AttemptedAt = DateTime.UtcNow;
        }

        private static string? Normalize(string? s)
            => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();
}

