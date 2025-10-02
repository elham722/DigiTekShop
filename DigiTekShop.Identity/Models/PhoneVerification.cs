public class PhoneVerification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private PhoneVerification() { }

    public static PhoneVerification Create(Guid userId, string codeHash, DateTime expiresAt)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(codeHash, nameof(codeHash));
        Guard.AgainstPastDate(expiresAt, () => DateTime.UtcNow, nameof(expiresAt));

        return new PhoneVerification
        {
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            Attempts = 0
        };
    }

    
    public void ResetCode(string newHash, DateTime newExpiresAt)
    {
        Guard.AgainstNullOrEmpty(newHash, nameof(newHash));
        Guard.AgainstPastDate(newExpiresAt, () => DateTime.UtcNow, nameof(newExpiresAt));

        CodeHash = newHash;
        ExpiresAt = newExpiresAt;
        Attempts = 0;
    }

    public void IncrementAttempts(int maxAttempts = 5)
    {
        if (Attempts >= maxAttempts)
            throw new MaxAttemptsExceededException(UserId, maxAttempts);

        Attempts++;
    }

    public void ResetAttempts() => Attempts = 0;

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}