namespace DigiTekShop.SharedKernel.Enums.Verification;

public enum VerificationPurpose : byte
{
    Login = 1,
    Signup = 2,
    ResetPassword = 3,
    ChangePhone = 4,
    ChangeEmail = 5,
    TwoFactorAuth = 6,
    AccountRecovery = 7
}

