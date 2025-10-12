namespace DigiTekShop.Contracts.DTOs.Auth.PhoneVerification
{
    public record PhoneVerificationStatsDto(
        int TotalCodes,
        int VerifiedCodes,
        int ExpiredCodes,
        int FailedAttempts,
        DateTime? LastVerificationAt
    )
    {
        public double SuccessRate => TotalCodes > 0 ? (double)VerifiedCodes / TotalCodes * 100 : 0;
    }
}
