namespace DigiTekShop.Identity.Models;
    public class LoginAttempt
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid? UserId { get; private set; }
        public DateTime AttemptedAt { get; private set; } = DateTime.UtcNow;
        public LoginStatus Status { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }

        private LoginAttempt() { }

     public static LoginAttempt Create(Guid? userId,LoginStatus status, string? ipAddress = null, string? userAgent = null)
        {
            Guard.AgainstEmpty(status,nameof(status));
            return new LoginAttempt
            {
                UserId = userId,
                Status = status,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AttemptedAt = DateTime.UtcNow
            };
        }
    }

