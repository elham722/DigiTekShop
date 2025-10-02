namespace DigiTekShop.Identity.Models;

public class PasswordHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public DateTime ChangedAt { get; private set; } = DateTime.UtcNow;
    public User User { get; private set; } = default!;

    private PasswordHistory() { }

    public static PasswordHistory Create(Guid userId, string passwordHash)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(passwordHash, nameof(passwordHash));
        
        return new PasswordHistory 
        { 
            UserId = userId, 
            PasswordHash = passwordHash 
        };
    }
}