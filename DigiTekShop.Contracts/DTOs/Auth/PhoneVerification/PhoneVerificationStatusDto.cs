namespace DigiTekShop.Contracts.DTOs.Auth.PhoneVerification;

public class PhoneVerificationStatusDto
{
    public bool IsVerified { get; set; }

    public bool IsExpired { get; set; }

    public int Attempts { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public bool CanResend { get; set; }
}
