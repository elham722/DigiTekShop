namespace DigiTekShop.Contracts.Auth.PhoneVerification;

public record PhoneVerificationStatusDto(
    bool IsVerified,
    bool IsExpired,
    int Attempts,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? VerifiedAt,
    bool CanResend
);
