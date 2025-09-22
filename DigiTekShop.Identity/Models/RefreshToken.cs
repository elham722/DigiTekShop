
namespace DigiTekShop.Identity.Models
{
    public class RefreshToken
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Token { get; private set; } = default!;
        public DateTime ExpiresAt { get; private set; }
        public bool IsRevoked { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        // ارتباط با User
        public string UserId { get; private set; } = default!;
        public User User { get; private set; } = default!;

        private RefreshToken() { } // EF Core
        public RefreshToken(string token, DateTime expiresAt, string userId)
        {
            Token = token;
            ExpiresAt = expiresAt;
            UserId = userId;
        }

        public void Revoke() => IsRevoked = true;
    }
}
