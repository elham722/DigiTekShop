namespace DigiTekShop.SharedKernel.Enums.Security;


public enum SecurityEventType
{
    // Authentication Events
    LoginFailed,
    LoginBlocked,
    AccountLocked,
    AccountUnlocked,
    
    // MFA Events
    MfaFailed,
    MfaBypassAttempt,
    MfaDisabled,
    MfaEnabled,
    
    // Token Events
    RefreshTokenAnomaly,
    TokenReplay,
    TokenExpired,
    InvalidToken,
    
    // Device Events
    DeviceUntrusted,
    DeviceTrusted,
    DeviceSuspicious,
    DeviceBlocked,
    DeviceLimitExceeded,
    
    // Password Events
    PasswordResetRequested,
    PasswordResetFailed,
    PasswordChanged,
    PasswordHistoryViolation,
    
    // Permission Events
    PermissionDenied,
    UnauthorizedAccess,
    RoleChanged,
    PermissionChanged,
    
    // System Events
    SystemIntrusion,
    DataBreach,
    ConfigurationChanged,
    SecurityPolicyViolation,
    
    // Phone Verification Events
    PhoneVerificationFailed,
    PhoneVerificationBlocked,
    PhoneCodeReuse,
    
    // Rate Limiting Events
    RateLimitExceeded,
    BruteForceAttempt,
    SuspiciousActivity,
    
    // Step-Up Authentication Events
    StepUpMfaRequired,
    StepUpMfaFailed,
    StepUpMfaCompleted
}