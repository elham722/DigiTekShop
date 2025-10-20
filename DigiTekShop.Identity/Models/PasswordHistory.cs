namespace DigiTekShop.Identity.Models;

public sealed class PasswordHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = default!;

    public DateTime ChangedAtUtc { get; private set; }

    public User User { get; private set; } = default!;

    private PasswordHistory() { }


    public static PasswordHistory Create(Guid userId, string passwordHash, DateTime changedAtUtc)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(passwordHash, nameof(passwordHash));

       
        return new PasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash,
            ChangedAtUtc = changedAtUtc
        };
    }
}