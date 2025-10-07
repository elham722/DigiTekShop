public class PhoneVerification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    public string? PhoneNumber { get; private set; }
    public bool IsVerified { get; private set; } = false;
    public DateTime? VerifiedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private PhoneVerification() { }

    public static PhoneVerification Create(
        Guid userId, 
        string codeHash, 
        DateTime expiresAt,
        string? phoneNumber = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(codeHash, nameof(codeHash));
        Guard.AgainstPastDate(expiresAt, () => DateTime.UtcNow, nameof(expiresAt));

        return new PhoneVerification
        {
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            Attempts = 0,
            PhoneNumber = phoneNumber,
            IpAddress = ipAddress,
            UserAgent = userAgent
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

  
    public void MarkAsVerified()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        ResetAttempts();
    }

    public void UpdateRequestInfo(string? ipAddress = null, string? userAgent = null)
    {
        if (!string.IsNullOrWhiteSpace(ipAddress))
            IpAddress = ipAddress;
        
        if (!string.IsNullOrWhiteSpace(userAgent))
            UserAgent = userAgent;
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    
    public bool IsValid => !IsExpired() && !IsVerified && Attempts < 5;
}